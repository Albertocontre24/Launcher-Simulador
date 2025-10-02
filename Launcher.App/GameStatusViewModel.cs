namespace Launcher.App
{
    public class GameStatusViewModel
    {
        public string VersionInstalada { get; set; } = "1.2.0";
        public string UltimaVersion { get; set; } = "1.2.0";

        public string IconoEstado
        {
            get
            {
                return VersionInstalada == UltimaVersion
                    ? "Assets/tick.png"
                    : "Assets/warning.png";
            }
        }

        public string TextoEstado
        {
            get
            {
                return VersionInstalada == UltimaVersion
                    ? "El launcher está actualizado y listo para jugar"
                    : "Hay una actualización disponible";
            }
        }
    }
}
