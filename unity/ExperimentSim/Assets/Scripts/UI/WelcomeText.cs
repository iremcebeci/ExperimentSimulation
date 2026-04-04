using System;

public static class WelcomeText
{
    public static string BuildRoleMessage(string roleName)
    {
        string r = (roleName ?? "").Trim().ToLowerInvariant();

        if (r.Contains("admin") || r.Contains("yönetici") || r.Contains("yonetici"))
            return "Yönetici Paneline hoş geldiniz. Kullanıcıları, rolleri ve içerikleri yönetebilir, sistem ayarlarını düzenleyebilirsiniz.";

        if (r.Contains("teacher") || r.Contains("öğretmen") || r.Contains("ogretmen"))
            return "Öğretmen Paneline hoş geldiniz. Sınıflarınızı yönetebilir, öğrenci performansını takip edebilir ve deney/ödev atayabilirsiniz.";

        if (r.Contains("student") || r.Contains("öğrenci") || r.Contains("ogrenci"))
            return "Öğrenci Paneline hoş geldiniz. Deneyleri görüntüleyebilir, verilen ödevleri takip edip tamamlayabilirsiniz.";

        if (r.Contains("contentcreator") || r.Contains("content creator") || r.Contains("içerik") || r.Contains("icerik"))
            return "İçerik Paneline hoş geldiniz. Yeni deney içerikleri oluşturabilir, düzenleyebilir ve yayımlayabilirsiniz.";

        return "Panele hoş geldiniz.";
    }
}