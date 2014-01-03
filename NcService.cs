namespace NamedPipeToComMapper
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;

    using Maklerzentrum.NamedPipeToComMapper;

    public class NcService
    {
        private readonly List<Mapper> mappers = new List<Mapper>();

        public void Start()
        {
            foreach (var m in Settings.Default.Properties.OfType<SettingsProperty>()
                .Where(p => p.Name.StartsWith("Connection"))
                .Select(p => p.Name).Select(p => new Mapper(Settings.Default[p].ToString())))
            {
                this.mappers.Add(m);
                m.Start();
            }
        }

        public void Stop()
        {
            foreach (var m in this.mappers)
            {
                m.Stop();
            }
        }
    }
}
