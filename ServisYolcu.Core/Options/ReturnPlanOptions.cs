namespace ServisYolcu.Core.Options;

public class ReturnPlanOptions
{
    public const string SectionName = "ReturnPlan";

    /// <summary>IANA zaman dilimi kimliği. Gün sınırları ve hatırlatma saati buna göre hesaplanır.</summary>
    public string TimeZone { get; set; } = "Europe/Istanbul";

    /// <summary>
    /// Kararsız (Planned) yolculara hatırlatma bildiriminin gönderileceği yerel saat ("HH:mm").
    /// Yumuşak cutoff: bu saatten sonra da değişikliğe izin verilir, yalnızca hatırlatma gider.
    /// </summary>
    public string ReminderLocalTime { get; set; } = "15:00";

    /// <summary>
    /// Hatırlatma saatinden sonra, hatırlatmanın hâlâ gönderilebileceği süre. Servis yeniden
    /// başlatılırsa veya bir tur kaçırılırsa pencere içinde telafi edilir.
    /// </summary>
    public int ReminderWindowMinutes { get; set; } = 120;

    /// <summary>Arka plan servisinin kontrol sıklığı.</summary>
    public int PollIntervalMinutes { get; set; } = 5;

    /// <summary>false ise hatırlatma arka plan servisi hiç çalışmaz.</summary>
    public bool ReminderEnabled { get; set; } = true;
}
