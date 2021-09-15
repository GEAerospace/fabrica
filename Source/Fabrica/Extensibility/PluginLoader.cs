// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System;
using System.Text.RegularExpressions;
using System.Threading;

// reference: https://code.msdn.microsoft.com/windowsdesktop/Creating-a-simple-plugin-b6174b62
namespace GEAviation.Fabrica.Extensibility
{
    /// <summary>
    /// The following is a class used for loading plugins.
    /// </summary>
    /// <typeparam name="PluginType">
    /// This parameter is the type that the plugin will implement/inherit from. 
    /// </typeparam>
    public class PluginLoader<PluginType> : MarshalByRefObject
    {

        string mLoadPath = string.Empty;
        string[] mFileNames = null;
        List<AppDomain> mDomains = new List<AppDomain>();

        /// <summary>
        /// The following is the constructor for the PluginLoader
        /// </summary>
        /// <param name="aLoadPath">
        /// This Parameter is the path to the plugin .dlls. 
        /// </param>
        public PluginLoader(string aLoadPath, Regex aFileType, Func<Type, bool> aFilter = null)
        {
            string lError = string.Empty;

            // The following if block is to allow a empty PluginLoader is created for proxies.  
            if (!string.IsNullOrEmpty(aLoadPath))
            {
                mLoadPath = aLoadPath;

                TypeFilterDelegate = aFilter;
                if (Directory.Exists(mLoadPath))
                {

                    mFileNames = Directory.GetFiles(mLoadPath)
                            .Where(path => aFileType.IsMatch(path)).ToArray();
                    if (mFileNames.Length < 1)
                    {
                        lError = string.Format("NO plugins found in directory {0}",mLoadPath);
                        throw new FileNotFoundException(lError);
                    }

                }
                else
                {
                    lError = string.Format("{0} does not exist ", mLoadPath);
                    throw new FileNotFoundException(lError);
                }
#if !NETSTANDARD
                mFileNames = checkProxyAssemblies(mFileNames);
#endif
            }


        }

        /// <summary>
        /// The Following deconstructor unloads all appdomains in the plugin loader. 
        /// </summary>
        ~PluginLoader()
        {
            if (mDomains != null)
            {
                foreach (AppDomain iDomain in mDomains)
                {
                    try
                    {
                        AppDomain.Unload(iDomain);
                    }
                    catch (CannotUnloadAppDomainException)
                    {
                        const int cMaxSeconds = 3;

                        // The following code waits for a domain to quit
                        // running if initial unload fails.                        
                        for (int iCounter = 0; iCounter < cMaxSeconds; iCounter++)
                        {
                            Thread.Sleep(1000);
                            try
                            {
                                AppDomain.Unload(iDomain);
                            }
                            catch
                            {
                                continue;
                            }
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// This property is a delegate to allow the user to specify specific attributes. 
        /// </summary>
        public Func<Type, bool> TypeFilterDelegate { get; set; }

        /// <summary>
        /// This property is an array of all domains created in this plugin loader.
        /// </summary>
        public AppDomain[] Domains
        {
            get
            {
                return mDomains.ToArray();
            }
        }

        /// <summary>
        /// This property is the number of files that the load path contains. 
        /// This number changes if check assemblies have been ran. 
        /// </summary>
        public int TotalFiles
        {
            get
            {
                return mFileNames.Length;
            }
        }

        /// <summary>
        /// This property is a string array of all the files in the load path. 
        /// </summary>
        public string[] FileNames
        {
            set
            {
                mFileNames = value;
            }
        }

#if !NETSTANDARD
        /// <summary>
        /// This function loads a sandbox appdomain and loads all assemblies in said domain,
        /// and checks to ensure assemblies fit the criteria for loading.
        /// </summary>
        /// <param name="aFiles">
        /// This parameter is the array of all files in the loadpath. 
        /// </param>
        /// <returns>
        /// This function returns a new set of files in the loadpath 
        /// that have assemblies that fulfill the correct criteria.
        /// </returns>
        protected string[] checkProxyAssemblies(string[] aFiles)
        {
            AppDomain lTempDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());

            PluginLoader<PluginType> lTempLoader = createProxyLoader(lTempDomain);

            string[] lReturn = lTempLoader.checkAssemblies(aFiles);

            AppDomain.Unload(lTempDomain);

            return lReturn;
        }
#endif

        /// <summary>
        /// This function loads all assemblies,
        /// and checks to ensure assemblies fit the criteria for loading.
        /// </summary>
        /// <param name="aFiles">
        /// This parameter is the array of all files in the loadpath. 
        /// </param>
        /// <returns>
        /// This function returns a new set of files in the loadpath 
        /// that have assemblies that fulfill the correct criteria.
        /// </returns>
        protected string[] checkAssemblies(string[] aFiles)
        {

            ICollection<Assembly> lAssemblys = new List<Assembly>(aFiles.Length);
            Type lInterfaceType = typeof(PluginType);
            ICollection<Type> lPluginTypes = new List<Type>();
            List<string> lReturnFiles = new List<string>();

            // The following loads all assemblys from the plugin path.
            foreach (string iFileName in aFiles)
            {
                try
                {
                    AssemblyName lAssemblyName = AssemblyName.GetAssemblyName(iFileName);
                    Assembly iPlugin = Assembly.Load(lAssemblyName);

                    if (iPlugin != null)
                    {
                        Type[] iTypes = iPlugin.GetTypes();

                        //The following loops through all types in the plugin 
                        //looking for matches to type 'T'.
                        foreach (Type iSingleType in iTypes)
                        {

                            if (!(iSingleType.IsInterface || iSingleType.IsAbstract))
                            {
                                if (lInterfaceType.IsAssignableFrom(iSingleType))
                                {
                                    if (TypeFilterDelegate == null || TypeFilterDelegate(iSingleType))
                                    {
                                        lReturnFiles.Add(iFileName);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //TODO > Add some error logging? 
                }

            }
            return lReturnFiles.ToArray();
        }

       /// <summary>
       /// The following function loads the plugins found in the file.
       /// </summary>
       /// <param name="aFile">
       /// This parameter is the file from which the types will be loaded. 
       /// </param>
       /// <returns>
       /// A 'Collection' of types from the plugin. 
       /// </returns>
        protected IEnumerable<Type> loadTypes(string aFile)
        {

            Type lInterfaceType = typeof(PluginType);
            ICollection<Type> lPluginTypes = new List<Type>();

            try
            {
                AssemblyName lAssemblyName = AssemblyName.GetAssemblyName(aFile);

                Assembly lAssembly = Assembly.Load(lAssemblyName);

                if (lAssembly != null)
                {
                    Type[] lTypes = lAssembly.GetTypes();

                    //The following loops through all types in the plugin 
                    //looking for matches to type 'T'.
                    foreach (Type iSingleType in lTypes)
                    {

                        if (!(iSingleType.IsInterface || iSingleType.IsAbstract))
                        {
                            if (lInterfaceType.IsAssignableFrom(iSingleType))
                            {
                                if (TypeFilterDelegate == null || TypeFilterDelegate(iSingleType))
                                {
                                    lPluginTypes.Add(iSingleType);
                                }
                            }
                        }
                    }
                }
            }

            catch
            {
               //TODO > Add some error logging?
            }

            return lPluginTypes;

        }

        /// <summary>
        /// The following function loads the plugins found in the directory. 
        /// </summary>
        /// <returns>
        /// A 'Collection' of types from the plugins.  
        /// </returns>
        public IEnumerable<Type> loadAllTypes()
        {
            List<Type> lPluginTypes = new List<Type>();

            foreach (string iFileName in mFileNames)
            {
                lPluginTypes.AddRange(loadTypes(iFileName));
            }

            return (IEnumerable<Type>)lPluginTypes;
        }

        /// <summary>
        /// This function Loads all plugins.
        /// </summary>
        /// <returns>
        /// A 'Collection' of 'T' from the plugins. 
        /// If type doesn't have default constructor Doesn't add to collection 
        /// </returns>
        public IEnumerable<PluginType> load()
        {
            List<PluginType> lReturnPlugins = new List<PluginType>();

            for (int iCounter = 0; iCounter < mFileNames.Length; iCounter++)
            {
                lReturnPlugins.AddRange(loadSingleFile(mFileNames[iCounter]));
            }

            return lReturnPlugins;
        }
        
        /// <summary>
        /// This function loads the plugins found in the file.
        /// Throws exception if not a single type contains a default constructor.
        /// </summary>
        /// <param name="PluginType">
        /// This parameter is the type that the plugin inherits from/implements. 
        /// </param>
        /// <param name="aFile">
        /// This parameter is the file from which the types will be loaded. 
        /// </param>
        /// <returns>
        /// A 'Collection' of 'PluginType' objects from the plugin. 
        /// If type doesn't have default constructor Doesn't add to collection 
        /// </returns>
        protected IEnumerable<PluginType> loadSingleFile(string aFile)
        {
            List<PluginType> lReturnPlugins = new List<PluginType>();
            IEnumerable<Type> lPluginTypes = loadTypes(aFile);

            for (int iTypeCounter = 0; iTypeCounter < lPluginTypes.ToArray<Type>().Length; iTypeCounter++)
            {
                Type lCurrentType = lPluginTypes.ToArray<Type>()[iTypeCounter];
                ConstructorInfo lDefaultConstructor = lCurrentType.GetConstructor(Type.EmptyTypes);
                
                if (lDefaultConstructor != null)
                {
                    lReturnPlugins.Add((PluginType)Activator.CreateInstance(lCurrentType));
                }
            }         
            return lReturnPlugins;
        }

#if !NETSTANDARD

        /// <summary>
        /// This function creates a single different appDomain,
        /// then loads the plugins into appDomain.
        /// Throws exception if not a single type contains a default constructor.
        /// </summary>
        /// <param name="PluginType">
        /// This parameter is the type that the plugin inherits from/implements. 
        /// </param>
        /// <returns>
        /// A 'Collection' of 'T' objects from the plugin. 
        /// If type doesn't have default constructor Doesn't add to collection 
        /// </returns>
        public IEnumerable<PluginType> loadAssembliesInDifferentDomain()
        {
            AppDomain lNewDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());

            PluginLoader<PluginType> lProxyLoader = createProxyLoader(lNewDomain);

            mDomains.Add(lNewDomain);

            return lProxyLoader.load();
        }

        /// <summary>
        /// The following function creates a different appDomain for each assembly,
        /// then loads the plugins into appDomain.
        /// Throws exception if not a single type contains a default constructor.
        /// </summary>
        /// <param name="PluginType">
        /// This parameter is the type that the plugin inherits from/implements. 
        /// </param>
        /// <returns> 
        /// A 'Collection' of 'T' objects from the plugin. 
        /// If type doesn't have default constructor Doesn't add to collection 
        /// </returns>
        public IEnumerable<PluginType> loadEachAssemblyInOwnDomain()
        {
            List<PluginType> lPluginTypes = new List<PluginType>();

            foreach (string iFileName in mFileNames)
            {

                AppDomain lNewDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString());
                PluginLoader<PluginType> lNewProxy = createProxyLoader(lNewDomain);
                mDomains.Add(lNewDomain);
                lPluginTypes.AddRange(lNewProxy.loadSingleFile(iFileName));

            }

            return (IEnumerable<PluginType>)lPluginTypes;
        }

        /// <summary>
        /// This function is used to create a plugin loader that is a proxy in a different appdomain.
        /// </summary>
        /// <param name="PluginType">
        /// This parameter is the type that the plugin inherits from/implements. 
        /// </param>
        /// <param name="aDomain">
        /// This parameter is the domain to create the plugin loader is in. 
        /// </param>
        /// <returns>
        /// A plugin loader. 
        /// </returns>
        protected PluginLoader<PluginType> createProxyLoader(AppDomain aDomain)
        {
            ProxyPluginLoader<PluginType> lProxyWrapper = (ProxyPluginLoader<PluginType>)aDomain.CreateInstanceAndUnwrap(
                                                    typeof(ProxyPluginLoader<PluginType>).Assembly.FullName,
                                                    typeof(ProxyPluginLoader<PluginType>).FullName);

            PluginLoader<PluginType> lProxyLoader = lProxyWrapper.PluginLoader;
            lProxyLoader.FileNames = this.mFileNames;
                lProxyLoader.TypeFilterDelegate = this.TypeFilterDelegate;
            return lProxyLoader;
        }

#endif

        /// <summary>
        /// The Class is a private wrapper class for a plugin loader.
        /// This is used to create a plugin loader without a loadpath 
        /// </summary>
        /// <typeparam name="GenericType">
        /// The param type is the Interface that the plugin will implement. 
        /// </typeparam>
        private class ProxyPluginLoader<GenericType> : MarshalByRefObject
        {
            public PluginLoader<GenericType> PluginLoader
            {
                get
                {
                    return new PluginLoader<GenericType>(string.Empty, null);
                }
            }
        }

    }

}
