using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace ResourceEmbedder.Core.Cecil
{
    internal class DirectoryAssemblyResolver : IAssemblyResolver
    {
        private readonly ILogger _logger;
        private readonly IReadOnlyCollection<string> _searchDirectories;
        private readonly Dictionary<string, AssemblyDefinition> _assemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>(StringComparer.InvariantCultureIgnoreCase);

        public DirectoryAssemblyResolver(ILogger logger, IReadOnlyCollection<string> searchDirectories)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _searchDirectories = searchDirectories ?? throw new ArgumentNullException(nameof(searchDirectories));
        }

        public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference)
        {
            return Resolve(assemblyNameReference, new ReaderParameters());
        }

        public AssemblyDefinition Resolve(AssemblyNameReference assemblyNameReference, ReaderParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var assemblyFileName = assemblyNameReference.Name + ".dll";

            foreach (var searchDirectory in _searchDirectories)
            {
                var assemblyPath = Path.Combine(searchDirectory, assemblyFileName);

                if (_assemblyDefinitionCache.TryGetValue(assemblyPath, out var assemblyDefinition))
                {
                    return assemblyDefinition;
                }

                if (File.Exists(assemblyPath))
                {
                    _logger.Info($"Resolving {assemblyNameReference} to {assemblyPath}");
                    parameters.AssemblyResolver = parameters.AssemblyResolver ?? this;
                    return _assemblyDefinitionCache[assemblyPath] = AssemblyDefinition.ReadAssembly(assemblyPath, parameters);
                }
            }

            LogErrorMessage(assemblyNameReference, assemblyFileName);

            return null;
        }

        private void LogErrorMessage(AssemblyNameReference assemblyNameReference, string assemblyFileName)
        {
            string errorMessage;
            if (_searchDirectories.Count == 0)
            {
                errorMessage = $@"Failed to resolve {assemblyNameReference} because no search directories were supplied.";
            }
            else if (_searchDirectories.Count == 1)
            {
                errorMessage = $@"Failed to resolve {assemblyNameReference} because the {assemblyFileName} file was not found in ""{_searchDirectories.Single()}"".";
            }
            else
            {
                var searchDirectoriesList = _searchDirectories.Aggregate("", (current, next) => current + Environment.NewLine + "  - " + next);
                errorMessage = $@"Failed to resolve {assemblyNameReference}
The following {_searchDirectories.Count} directories were searched but {assemblyFileName} was not found in either of them:{searchDirectoriesList}";
            }

            _logger.Error(errorMessage);
        }

        public void Dispose()
        {
            foreach (var value in _assemblyDefinitionCache.Values)
            {
                value?.Dispose();
            }
        }
    }
}
