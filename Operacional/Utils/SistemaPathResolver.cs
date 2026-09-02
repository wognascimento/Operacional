using Operacional.DataBase;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Operacional.Utils
{
    public static class SistemaPathResolver
    {
        public static string GetImpressosPath(string fileName)
        {
            var basePath = GetBasePath();
            var impressosPath = Path.Combine(basePath, "Impressos");
            Directory.CreateDirectory(impressosPath);
            return Path.Combine(impressosPath, fileName);
        }

        public static string GetModeloPath(string fileName)
        {
            var candidates = new List<string>
            {
                Path.Combine(GetBasePath(), "Modelos", fileName),
                Path.Combine(AppContext.BaseDirectory, "Modelos", fileName)
            };

            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null)
            {
                candidates.Add(Path.Combine(current.FullName, "Modelos", fileName));
                current = current.Parent;
            }

            var modeloPath = candidates.FirstOrDefault(File.Exists);
            if (!string.IsNullOrWhiteSpace(modeloPath))
                return modeloPath;

            throw new FileNotFoundException(
                $"Modelo '{fileName}' nao encontrado. Locais verificados:{Environment.NewLine}{string.Join(Environment.NewLine, candidates.Distinct())}",
                fileName);
        }

        public static void OpenFile(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"Arquivo nao encontrado: {path}", path);

            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        public static void OpenInExplorer(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                throw new FileNotFoundException($"Arquivo ou diretorio nao encontrado: {path}", path);

            Process.Start("explorer", path);
        }

        private static string GetBasePath()
        {
            var configuredPath = DataBaseSettings.Instance.CaminhoSistema;
            return string.IsNullOrWhiteSpace(configuredPath)
                ? AppContext.BaseDirectory
                : configuredPath;
        }
    }
}
