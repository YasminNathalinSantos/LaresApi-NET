namespace LaresApi.Domain
{
    public static class DataHora
    {
        private static readonly TimeZoneInfo Fuso = ObterFuso();

        public static DateTime AgoraUtc(TimeProvider tempo)
        {
            var utc = tempo.GetUtcNow().UtcDateTime;
            return new DateTime(utc.Ticks - (utc.Ticks % TimeSpan.TicksPerSecond), DateTimeKind.Utc);
        }

        public static DateTime ParaBrasilia(DateTime data)
        {
            var utc = DateTime.SpecifyKind(data, DateTimeKind.Utc);
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, Fuso);
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        }

        private static TimeZoneInfo ObterFuso()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            }
            catch (Exception)
            {
                return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
            }
        }
    }

    public static class NovoId
    {
        public static string Gerar() => MongoDB.Bson.ObjectId.GenerateNewId().ToString();
    }
}
