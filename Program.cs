namespace NamedPipeToComMapper
{
    using Topshelf;

    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            HostFactory.Run(
                host =>
                {
                    host.Service<NcService>(
                        sc =>
                            {
                                sc.ConstructUsing(name => new NcService());
                                sc.WhenStarted(nc => nc.Start());
                                sc.WhenStopped(nc => nc.Stop());
                            });

                    host.RunAsNetworkService();
                    host.SetServiceName("NamedPipeToComMapper");
                    host.SetDisplayName("Named pipe to COM-port mapper");
                });
        }
    }
}
