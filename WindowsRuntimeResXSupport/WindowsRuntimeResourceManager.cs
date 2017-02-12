// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowsRuntimeResourceManager.cs" company="">
//   Copyright (c) 2015
// </copyright>
// <summary>
//   A <see cref="ResourceManager" /> which works for Windows RT and can retrieve resources from Shared projects, if
//   they exist there both as a ResX file and a TXT file.
//   <para />
//   To get this working, you have to add a TXT file to the folder, where the ResX file is located. To use different
//   cultures in ResX, you suffix the file name and prefix the file extension with the culture code. You should do the
//   same for
//   the TXT file, but you have to use an underscore instead of a period. Additionally, TXT file has to be an
//   EmbeddedResource.
//   <para />
//   An example project tree:
//   Shared
//   -Resources
//   --LocalizationResources.resx
//   --LocalizationResources.txt
//   --LocalizationResources.en.resx
//   --LocalizationResources_en.txt
//   --LocalizationResources.de.resx
//   --LocalizationResources_de.txt
//   <para />
//   Note, that a ResX file without language code is treated as <see cref="CultureInfo.InvariantCulture" /> which may
//   differ from your neutral culture.
//   <para />
//   To simplify this structure, use the following pre-build command for every ResX file (replace [Shared] with the
//   actual name of
//   your Shared project):
//   <para />
//   "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ResGen.exe"
//   "$(SolutionDir)$(SolutionName)\[Shared]\[ResX file]" "$(SolutionDir)$(SolutionName)\[Shared]\[Text file]"
//   <para />
//   Finally you have to call either <see cref="InjectIntoResxGeneratedApplicationResourcesClass(Type)" /> or
//   <see cref="WindowsRuntimeResourceManager(Type)" />/<see cref="WindowsRuntimeResourceManager(string, Assembly)" />
//   inject a new instance of the <see cref="WindowsRuntimeResourceManager" /> into a ResX designer class.
//   <para />
//   This class was set up because the <see cref="Windows.ApplicationModel.Resources.Core.ResourceManager" /> threw an
//   <see cref="MissingManifestResourceException" /> when accessing
//   ResX files from the Shared project.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace WindowsRuntimeResXSupport
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Resources;

    /// <summary>
    ///     A <see cref="ResourceManager" /> which works for Windows RT and can retrieve resources from Shared projects, if
    ///     they exist there both as a ResX file and a TXT file.
    ///     <para />
    ///     To get this working, you have to add a TXT file to the folder, where the ResX file is located. To use different
    ///     cultures in ResX, you suffix the file name and prefix the file extension with the culture code. You should do the
    ///     same for
    ///     the TXT file, but you have to use an underscore instead of a period. Additionally, TXT file has to be an
    ///     EmbeddedResource.
    ///     <para />
    ///     An exemplary project tree:
    ///     Shared
    ///     -Resources
    ///     --LocalizationResources.resx
    ///     --LocalizationResources.txt
    ///     --LocalizationResources.en.resx
    ///     --LocalizationResources_en.txt
    ///     --LocalizationResources.de.resx
    ///     --LocalizationResources_de.txt
    ///     <para />
    ///     Note, that a ResX file without language code is treated as <see cref="CultureInfo.InvariantCulture" /> which may
    ///     differ from your neutral culture.
    ///     <para />
    ///     To simplify this structure, use the following pre-build command for every ResX file (replace [Shared] with the
    ///     actual name of
    ///     your Shared project):
    ///     <para />
    ///     "C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ResGen.exe"
    ///     "$(SolutionDir)$(SolutionName)\[Shared]\[ResX file]" "$(SolutionDir)$(SolutionName)\[Shared]\[Text file]"
    ///     <para />
    ///     Finally you have to call either <see cref="InjectIntoResxGeneratedApplicationResourcesClass(Type)" /> or
    ///     <see cref="WindowsRuntimeResourceManager(Type)" />/<see cref="WindowsRuntimeResourceManager(string, Assembly)" />
    ///     inject a new instance of the <see cref="WindowsRuntimeResourceManager" /> into a ResX designer class.
    ///     <para />
    ///     This class was set up because the <see cref="Windows.ApplicationModel.Resources.Core.ResourceManager" /> threw an
    ///     <see cref="MissingManifestResourceException" /> when accessing
    ///     ResX files from the Shared project.
    /// </summary>
    public class WindowsRuntimeResourceManager : ResourceManager
    {
        /// <summary>
        ///     A <see cref="Dictionary{TKey,TValue}" /> holding the resources <see cref="Dictionary{TKey,TValue}" /> for each
        ///     <see cref="CultureInfo" />.
        /// </summary>
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> resourcesDictionary =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        /// <summary>
        ///     The culture of the resources.
        /// </summary>
        private CultureInfo culture;

        /// <summary>
        ///     The <see cref="Type" /> which contains the resource data for this instance of the
        ///     <see cref="WindowsRuntimeResourceManager" />.
        /// </summary>
        private Type resourcesSource;

        /// <summary>
        ///     Initializes static members of the <see cref="WindowsRuntimeResourceManager" /> class.
        /// </summary>
        static WindowsRuntimeResourceManager()
        {
            WindowsRuntimeResourceManager.CultureDelimiter = "_";
        }

        /// <inheritdoc />
        public WindowsRuntimeResourceManager(Type resourceSource)
            : base(resourceSource)
        {
            this.Initialize(resourceSource);
        }

        /// <inheritdoc />
        public WindowsRuntimeResourceManager(string baseName, Assembly assembly)
            : base(baseName, assembly)
        {
            this.Initialize(assembly.GetType(baseName));
        }

        /// <summary>
        ///     Gets or sets the delimeter used to seperate the resource name and the culture code in a file name. Default value is
        ///     '_'.
        /// </summary>
        public static string CultureDelimiter { get; set; }

        /// <summary>
        ///     Gets or sets the culture of the resources.
        /// </summary>
        public CultureInfo Culture
        {
            get
            {
                return this.culture;
            }

            set
            {
                this.culture = value;
                this.ResourcesSource.GetRuntimeProperty("Culture").SetValue(null, value);
            }
        }

        /// <summary>
        ///     Gets the <see cref="Type" /> which contains the resource data for this instance of the
        ///     <see cref="WindowsRuntimeResourceManager" /> (usually a ResX designer which contains this instance of the
        ///     <see cref="WindowsRuntimeResourceManager" /> class).
        /// </summary>
        public Type ResourcesSource
        {
            get
            {
                return this.resourcesSource;
            }

            set
            {
                this.resourcesSource = value;
                this.Initialize(value);
            }
        }

        /// <summary>
        /// Injects a new instance of the <see cref="WindowsRuntimeResourceManager"/> into the given ResX generated
        ///     application resources <see cref="Type"/>. This is done automatically in
        ///     <see cref="WindowsRuntimeResourceManager(Type)"/> or
        ///     <see cref="WindowsRuntimeResourceManager(string, Assembly)"/>, so use these methods to get a reference to the
        ///     generated instance.
        /// </summary>
        /// <param name="resxGeneratedApplicationResourcesClass">
        /// The ResX generated application resources <see cref="Type"/> to inject the new instance into.
        /// </param>
        public static void InjectIntoResxGeneratedApplicationResourcesClass(Type resxGeneratedApplicationResourcesClass)
        {
            new WindowsRuntimeResourceManager(resxGeneratedApplicationResourcesClass);
        }

        /// <inheritdoc />
        public override string GetString(string name, CultureInfo culture)
        {
            if (this.resourcesDictionary.ContainsKey(culture))
            {
                if (this.resourcesDictionary[culture].ContainsKey(name))
                {
                    return this.resourcesDictionary[culture][name];
                }
            }
            else if (this.resourcesDictionary.ContainsKey(new CultureInfo(culture.TwoLetterISOLanguageName)))
            {
                if (this.resourcesDictionary[new CultureInfo(culture.TwoLetterISOLanguageName)].ContainsKey(name))
                {
                    return this.resourcesDictionary[new CultureInfo(culture.TwoLetterISOLanguageName)][name];
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Gets the file name of the resource file represented by the given name of the resource <see cref="Type"/> for the
        ///     given <see cref="CultureInfo"/>.
        /// </summary>
        /// <param name="resourceSourceTypeName">
        /// The fully qualified name of the resource <see cref="Type"/> (eg 'Shared.Resources.LocalizationResources').
        /// </param>
        /// <returns>
        /// The file name of the resource file represented by the given name of the resource <see cref="Type"/> for the given
        ///     <see cref="CultureInfo"/>.
        /// </returns>
        private static IEnumerable<string> GetFileNamesForResourceType(string resourceSourceTypeName)
        {
            if (!resourceSourceTypeName.Contains('.'))
            {
                return null;
            }

            var resourceIdentifier = resourceSourceTypeName.Remove(0, resourceSourceTypeName.Split('.')[0].Length + 1);
            var pathComponents = resourceIdentifier.Split('.');
            var fileNameWithoutPath = resourceIdentifier.Remove(
                0,
                pathComponents.Take(pathComponents.Length - 1).Aggregate(0, (i, s) => s.Length) + 1);
            var resourceDirectory = string.Empty;
            if (resourceIdentifier.Length != fileNameWithoutPath.Length)
            {
                resourceDirectory = resourceIdentifier.Remove(
                    resourceIdentifier.Length - fileNameWithoutPath.Length - 1,
                    fileNameWithoutPath.Length + 1);
            }

            var defaultCultureFileName = $"{resourceIdentifier}.txt";
            var fileNames = new List<string> { defaultCultureFileName };
            fileNames.AddRange(
                WindowsRuntimeResourceManager.GetFileNamesInDirectory(resourceDirectory).
                    Where(
                        file =>
                        defaultCultureFileName != file && file.EndsWith(".txt")
                        && file.Contains(fileNameWithoutPath.Split('.')[0])));
            return fileNames;
        }

        /// <summary>
        /// Gets the files of a specified directory (with subdirectories) or a null reference if the specified directory does
        ///     not exist.
        /// </summary>
        /// <param name="directory">
        /// The fully qualified directory name in the project structure (which means, periods instad of slashes) and without
        ///     the namespace.
        /// </param>
        /// <returns>
        /// The files of a specified directory (with subdirectories).
        /// </returns>
        private static IEnumerable<string> GetFileNamesInDirectory(string directory)
        {
            var assembly = typeof(WindowsRuntimeResourceManager).GetTypeInfo().Assembly;
            var allFileNames =
                new List<string>(
                    assembly.GetManifestResourceNames().
                        Select(s => s.Remove(0, assembly.FullName.Split(',')[0].Length + 1)));
            if (directory == string.Empty)
            {
                return allFileNames;
            }

            return allFileNames.Where(fileName => fileName.StartsWith(directory));
        }

        /// <summary>
        /// Maps the given resource string into an <see cref="IDictionary{TKey,TValue}"/>.
        /// </summary>
        /// <param name="resources">
        /// The resource to be mapped in a .properties format ('key=value').
        /// </param>
        /// <returns>
        /// The given resource string as <see cref="IDictionary{TKey,TValue}"/>.
        /// </returns>
        private static IDictionary<string, string> GetResourceDictionary(string resources)
        {
            var dictionary = new Dictionary<string, string>();
            foreach (var line in resources.Split(new[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if ((!string.IsNullOrEmpty(line)) && (!line.StartsWith(";")) && (!line.StartsWith("#"))
                    && (!line.StartsWith("'")) && line.Contains('='))
                {
                    var index = line.IndexOf('=');
                    var key = line.Substring(0, index).Trim();
                    var value = line.Substring(index + 1).Trim().Replace(@"\r\n", Environment.NewLine);

                    if ((value.StartsWith("\"") && value.EndsWith("\""))
                        || (value.StartsWith("'") && value.EndsWith("'")))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }

        /// <summary>
        /// Gets the text content of a embedded resource.
        /// </summary>
        /// <param name="fileName">
        /// The fully qualified file name in the project structure (which means, periods instad of slashes) and without the
        ///     namespace.
        /// </param>
        /// <returns>
        /// The files contents as <see cref="string"/>.
        /// </returns>
        private static string ReadFile(string fileName)
        {
            var assembly = typeof(WindowsRuntimeResourceManager).GetTypeInfo().Assembly;
            using (var stream = assembly.GetManifestResourceStream($"{assembly.FullName.Split(',')[0]}.{fileName}"))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException(
                        "Specified file name is not valid. Check whether you used periods instead of slashes.",
                        fileName);
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// Initializes this instance of the <see cref="WindowsRuntimeResourceManager"/> class.
        /// </summary>
        /// <param name="resourceSource">
        /// The <see cref="Type"/> which contains the resource data for this instance of the
        ///     <see cref="WindowsRuntimeResourceManager"/>.
        /// </param>
        private void Initialize(Type resourceSource)
        {
            var resourcesFiles =
                new List<string>(WindowsRuntimeResourceManager.GetFileNamesForResourceType(resourceSource.FullName));
            foreach (var file in resourcesFiles)
            {
                var underscoreSplit = file.Split(
                    new[] { WindowsRuntimeResourceManager.CultureDelimiter },
                    StringSplitOptions.None);
                var cultureInfo = CultureInfo.InvariantCulture;
                if (file.Contains(WindowsRuntimeResourceManager.CultureDelimiter) && file.Contains('.'))
                {
                    cultureInfo = new CultureInfo(underscoreSplit[1].Split('.')[0]);
                }

                var resources = WindowsRuntimeResourceManager.ReadFile(file);
                if (!this.resourcesDictionary.ContainsKey(cultureInfo))
                {
                    this.resourcesDictionary.Add(
                        cultureInfo,
                        new Dictionary<string, string>(WindowsRuntimeResourceManager.GetResourceDictionary(resources)));
                }

                this.resourcesSource = resourceSource;
                this.InjectThisInstanceIntoResxGeneratedApplicationResourcesClass(resourceSource);
            }
        }

        /// <summary>
        /// Injects this instance of the <see cref="WindowsRuntimeResourceManager"/> into the given ResX generated application
        ///     resources <see cref="Type"/>.
        /// </summary>
        /// <param name="resxGeneratedApplicationResourcesClass">
        /// The ResX generated application resources <see cref="Type"/> to inject this instance into.
        /// </param>
        private void InjectThisInstanceIntoResxGeneratedApplicationResourcesClass(
            Type resxGeneratedApplicationResourcesClass)
        {
            resxGeneratedApplicationResourcesClass.GetRuntimeFields().
                First(m => m.Name == "resourceMan").
                SetValue(null, this);
        }
    }
}
