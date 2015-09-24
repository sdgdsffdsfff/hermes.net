using System.Reflection;

namespace Arch.CMessaging.Client.Impl
{
	internal class Version
	{
        private static string _version;

        public static string AssemblyVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                {
                    _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
                return _version;
            }
        }

	    private static string _majorVerion;
	    public  static string AssemblyMajorVersion
	    {
	        get
	        {
                if (string.IsNullOrEmpty(_majorVerion))
                {
                    var version = Assembly.GetExecutingAssembly().GetName().Version;
                    _majorVerion = version.Major.ToString() + "."+version.Minor.ToString();
                }
	            return _majorVerion;
	        }
	    }
	}
}
