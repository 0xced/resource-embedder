using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Mdb;
using Mono.Cecil.Pdb;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ResourceEmbedder.Core.Cecil
{
    /// <summary>
    /// Cecil based implementation of <see cref="IModifyAssemblies"/>
    /// </summary>
    public class CecilBasedAssemblyModifier : IModifyAssemblies
    {
        private readonly AssemblyDefinition _assemblyDefinition;
        private readonly IInjectCode _codeInjector;
        private readonly ILogger _logger;
        private readonly StrongNameKeyPair _signingKey;
        private readonly string _tempFilePath;
        private readonly string _tempSymbolFilePath;
        private readonly IEmbedResources _resourceEmbedder;
        private readonly ISymbolWriterProvider _symbolsWriter;

        /// <summary>
        /// Creates a new modifier that can insert resources and code into an assembly.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="inputAssembly"></param>
        /// <param name="outputAssembly"></param>
        /// <param name="searchDirectories"></param>
        /// <param name="debugSymbolType">Determines which (if any) debug symbols are read.</param>
        /// <param name="signingKey">Optional signing key to be applied to the output assembly.</param>
        public CecilBasedAssemblyModifier(ILogger logger, string inputAssembly, string outputAssembly, string[] searchDirectories = null, DebugSymbolType debugSymbolType = DebugSymbolType.Full, StrongNameKeyPair signingKey = null)
        {
            if (logger == null)
            {
                logger = new DummyLogger();
            }
            if (!File.Exists(inputAssembly))
            {
                throw new FileNotFoundException(inputAssembly);
            }
            if (string.IsNullOrEmpty(outputAssembly))
            {
                throw new ArgumentNullException("outputAssembly");
            }

            _logger = logger;
            _signingKey = signingKey;
            // cecil 0.10 has a lock on the read file now so need to copy it
            _tempFilePath = Path.ChangeExtension(Path.GetFullPath(inputAssembly), ".tmp");
            File.Copy(inputAssembly, _tempFilePath, true);

            var symbolExtension = "pdb";
            var existingSymbolsPath = Path.ChangeExtension(inputAssembly, symbolExtension);
            if (!File.Exists(existingSymbolsPath))
            {
                symbolExtension = "mdb";
                existingSymbolsPath = Path.ChangeExtension(inputAssembly, symbolExtension);
            }

            // symbols are optional
            if (File.Exists(existingSymbolsPath))
            {
                _tempSymbolFilePath = $"{Path.ChangeExtension(Path.GetFullPath(existingSymbolsPath), ".tmp")}.{symbolExtension}";
                File.Copy(existingSymbolsPath, _tempSymbolFilePath, true);
            }

            InputAssembly = inputAssembly = _tempFilePath;
            OutputAssembly = Path.GetFullPath(outputAssembly);

            ISymbolReaderProvider symbolReader = GetSymbolReader(_tempSymbolFilePath, debugSymbolType);
            _symbolsWriter = GetSymbolWriter(_tempSymbolFilePath, debugSymbolType);

            var rp = GetReaderParameters(inputAssembly, searchDirectories, symbolReader);

            _assemblyDefinition = AssemblyDefinition.ReadAssembly(inputAssembly, rp);
            _resourceEmbedder = new CecilBasedResourceEmbedder(logger);
            _codeInjector = new CecilBasedCodeInjector(logger, new DirectoryAssemblyResolver(logger, searchDirectories));
        }

        /// <summary>
        /// Given an existing assembly, this will check for the existance of PDB files
        /// </summary>
        /// <returns></returns>
        public static ISymbolReaderProvider GetSymbolReader(string assemblyPath, DebugSymbolType debugSymbolType = DebugSymbolType.Full)
        {
            var pdb = File.Exists(Path.ChangeExtension(assemblyPath, "pdb"));
            var mdb = File.Exists(Path.ChangeExtension(assemblyPath, "mdb"));

            if (!pdb && !mdb && debugSymbolType != DebugSymbolType.Embedded)
                return null;

            switch (debugSymbolType)
            {
                case DebugSymbolType.None:
                    return null;
                case DebugSymbolType.Embedded:
                    return new EmbeddedPortablePdbReaderProvider();
                case DebugSymbolType.Full:
                case DebugSymbolType.PdbOnly:
                case DebugSymbolType.Portable:
                    return pdb ? (ISymbolReaderProvider)new PdbReaderProvider() : new MdbReaderProvider();
                default:
                    throw new NotSupportedException(debugSymbolType.ToString());
            }
        }

        /// <summary>
        /// Given an existing assembly, this will check for the existance of PDB files
        /// </summary>
        /// <returns></returns>
        public static ISymbolWriterProvider GetSymbolWriter(string assemblyPath, DebugSymbolType debugSymbolType = DebugSymbolType.Full)
        {
            var pdb = File.Exists(Path.ChangeExtension(assemblyPath, "pdb"));
            var mdb = File.Exists(Path.ChangeExtension(assemblyPath, "mdb"));

            if (!pdb && !mdb && debugSymbolType != DebugSymbolType.Embedded)
                return null;

            switch (debugSymbolType)
            {
                case DebugSymbolType.None:
                    return null;
                case DebugSymbolType.Full:
                case DebugSymbolType.PdbOnly:
                    return pdb ? (ISymbolWriterProvider)new PdbWriterProvider() : new MdbWriterProvider();
                case DebugSymbolType.Embedded:
                case DebugSymbolType.Portable:
                    return new EmbeddedPortablePdbWriterProvider();
                default:
                    throw new NotSupportedException(debugSymbolType.ToString());
            }
        }

        /// <summary>
        /// Helper to create the correct reader parameter construct based on the parameters.
        /// </summary>
        /// <returns></returns>
        public static ReaderParameters GetReaderParameters(string inputAssembly, IEnumerable<string> searchDirectories, ISymbolReaderProvider symbolReader)
        {
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(new FileInfo(inputAssembly).DirectoryName);

            if (searchDirectories != null)
                foreach (var dir in searchDirectories)
                    resolver.AddSearchDirectory(dir);

            var rp = new ReaderParameters
            {
                AssemblyResolver = resolver,
                ReadSymbols = symbolReader != null,
                SymbolReaderProvider = symbolReader
            };
            return rp;
        }

        /// <inheritdoc />
        public string InputAssembly { get; }

        /// <inheritdoc />
        public string OutputAssembly { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Save();
        }

        /// <inheritdoc />
        public bool EmbedResources(ResourceInfo[] resourceInfo)
        {
            return _resourceEmbedder.EmbedResources(_assemblyDefinition, resourceInfo);
        }

        /// <inheritdoc />
        public bool InjectModuleInitializedCode(Func<AssemblyDefinition, IAssemblyResolver, MethodDefinition> func)
        {
            return _codeInjector.Inject(_assemblyDefinition, func);
        }

        /// <inheritdoc />
        public void Save()
        {
            if (_symbolsWriter != null)
            {
                _logger.Info($"Rewriting debug symbols with {_symbolsWriter.GetType().Name}");
            }
            _assemblyDefinition.Write(OutputAssembly, new WriterParameters
            {
#if !NETSTANDARD
                StrongNameKeyPair = _signingKey,
#endif
                WriteSymbols = _symbolsWriter != null,
                SymbolWriterProvider = _symbolsWriter
            });
            _assemblyDefinition.Dispose();
            File.Delete(_tempFilePath);
            if (!string.IsNullOrEmpty(_tempSymbolFilePath))
                File.Delete(_tempSymbolFilePath);
        }
    }
}
