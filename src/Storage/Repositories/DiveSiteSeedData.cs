using Models;

namespace Storage.Repositories;

internal static class DiveSiteSeedData
{
    public static IReadOnlyList<DiveSite> Create()
    {
        return
        [
            new DiveSite(
                DiveSiteId.FromInt(1),
                "Cirkewwa",
                "Malta",
                Latitude.FromDegrees(35.9979D),
                Longitude.FromDegrees(14.3297D),
                382D,
                226D),
            new DiveSite(
                DiveSiteId.FromInt(2),
                "Ghar Lapsi",
                "Malta",
                Latitude.FromDegrees(35.8275D),
                Longitude.FromDegrees(14.4447D),
                602D,
                532D),
            new DiveSite(
                DiveSiteId.FromInt(3),
                "Zurrieq Blue Grotto",
                "Malta",
                Latitude.FromDegrees(35.8194D),
                Longitude.FromDegrees(14.4576D),
                688D,
                542D),
            new DiveSite(
                DiveSiteId.FromInt(4),
                "Exiles Sliema",
                "Malta",
                Latitude.FromDegrees(35.9138D),
                Longitude.FromDegrees(14.5068D),
                768D,
                365D),
            new DiveSite(
                DiveSiteId.FromInt(5),
                "Dwejra Blue Hole",
                "Gozo",
                Latitude.FromDegrees(36.0489D),
                Longitude.FromDegrees(14.1889D),
                101D,
                120D),
            new DiveSite(
                DiveSiteId.FromInt(6),
                "Patrol Boat P33",
                "Malta",
                Latitude.FromDegrees(35.8658D),
                Longitude.FromDegrees(14.5755D),
                905D,
                458D),
            new DiveSite(
                DiveSiteId.FromInt(7),
                "Fra Ben Cave",
                "Malta",
                Latitude.FromDegrees(35.9601D),
                Longitude.FromDegrees(14.4277D),
                596D,
                288D),
            new DiveSite(
                DiveSiteId.FromInt(8),
                "Cathedral Cave",
                "Gozo",
                Latitude.FromDegrees(36.0811D),
                Longitude.FromDegrees(14.2300D),
                178D,
                68D),
            new DiveSite(
                DiveSiteId.FromInt(9),
                "Slugs Bay",
                "Malta",
                Latitude.FromDegrees(35.9797D),
                Longitude.FromDegrees(14.3612D),
                451D,
                256D),
            new DiveSite(
                DiveSiteId.FromInt(10),
                "Anchor",
                "Malta",
                Latitude.FromDegrees(35.9603D),
                Longitude.FromDegrees(14.3397D),
                404D,
                288D),
            new DiveSite(
                DiveSiteId.FromInt(11),
                "P31 Wreck",
                "Comino",
                Latitude.FromDegrees(36.0090D),
                Longitude.FromDegrees(14.3231D),
                375D,
                199D),
            new DiveSite(
                DiveSiteId.FromInt(12),
                "Xlendi Reef",
                "Gozo",
                Latitude.FromDegrees(36.0288D),
                Longitude.FromDegrees(14.2133D),
                144D,
                163D),
            new DiveSite(
                DiveSiteId.FromInt(13),
                "X127",
                "Malta",
                Latitude.FromDegrees(35.9009D),
                Longitude.FromDegrees(14.5038D),
                755D,
                395D),
        ];
    }
}
