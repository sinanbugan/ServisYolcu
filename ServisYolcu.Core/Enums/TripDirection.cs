namespace ServisYolcu.Core.Enums;

/// <summary>
/// Bir seferin hangi bacağı olduğunu belirtir. Dönüş seferi kendi Route kaydını
/// (iş yeri → ev, kendi sıralı durakları) gösterir; rota ters çevrilmez.
/// </summary>
public enum TripDirection
{
    Outbound = 0,
    Return = 1
}
