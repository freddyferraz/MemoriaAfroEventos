namespace TechFlow.Eventos.Core.Geo;

public readonly record struct Coordenadas(double Latitude, double Longitude)
{
    /// <summary>Distância em km em linha reta (fórmula de Haversine).</summary>
    public double DistanciaKm(Coordenadas outra)
    {
        const double raioTerraKm = 6371.0;
        static double Rad(double g) => g * Math.PI / 180.0;

        var dLat = Rad(outra.Latitude - Latitude);
        var dLng = Rad(outra.Longitude - Longitude);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(Rad(Latitude)) * Math.Cos(Rad(outra.Latitude))
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return raioTerraKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
