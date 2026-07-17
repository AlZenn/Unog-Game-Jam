using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Örnek DialogueData asset'lerini üretir: karakter başına 2 olumlu, 6 olumsuz havuz, 1 kapı.
// Var olan asset'lerin üzerine yazmaz — kullanıcı düzenlemeleri korunur.
public static class SampleDialogueGenerator
{
    public const string DialogueFolder = "Assets/Dialogues";

    public class DialogueLibrary
    {
        public Dictionary<string, List<DialogueData>> positivesByCharacter =
            new Dictionary<string, List<DialogueData>>();
        public List<DialogueData> negatives = new List<DialogueData>();
        public DialogueData door;
    }

    [MenuItem("Tools/UnoG/Örnek Diyalogları Üret", priority = 20)]
    public static void GenerateMenuItem()
    {
        EnsureAll();
        AssetDatabase.SaveAssets();
        Debug.Log("UnoG: Örnek diyaloglar hazır → " + DialogueFolder);
    }

    public static DialogueLibrary EnsureAll()
    {
        EnsureFolder();
        var lib = new DialogueLibrary();

        // ---- Kaos (özel stat: Öfke) ----
        lib.positivesByCharacter["Kaos"] = new List<DialogueData>
        {
            CreateOrLoad("Kaos_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "Sen de mi her şeyin yıkılmasını izliyorsun?"),
                    (true,  "Bazen yıkım, yeniden kurmanın tek yolu."),
                    (false, "Demek anlıyorsun... İçindeki ateşi hissediyorum."));
                Require(d, StatType.Ofke, 55, 100);
                Require(d, StatType.Cikar, 0, 45);
                Answers(d,
                    "Öfkeni anlıyorum, birlikte yakalım.", E(StatType.Ofke, 10),
                    "Ama önce nefes al, sakinleş.", E(StatType.Ofke, -10), E(StatType.Durustluk, 5));
            }),
            CreateOrLoad("Kaos_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "Ateş sönmeye başladı... İçim garip bir şekilde hafif."),
                    (true,  "Belki kaos da dinlenmeyi hak ediyordur."));
                Require(d, StatType.Ofke, 65, 100);
                Answers(d,
                    "Yıkmadan da var olabilirsin.", E(StatType.Ofke, -10),
                    "Ateşin hep yanmalı ama seni yakmadan.", E(StatType.Ofke, 5), E(StatType.Cikar, -5));
            }),
        };

        // ---- Merhamet (özel stat: Dürüstlük) ----
        lib.positivesByCharacter["Merhamet"] = new List<DialogueData>
        {
            CreateOrLoad("Merhamet_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "Herkes acı çekiyor ama kimse görmüyor..."),
                    (true,  "Ben görüyorum. Anlatmak ister misin?"),
                    (false, "Kalbin temizmiş. Sana güvenebilirim sanırım."));
                Require(d, StatType.Durustluk, 55, 100);
                Require(d, StatType.Ofke, 0, 45);
                Answers(d,
                    "Acıyı paylaşmak yükü hafifletir.", E(StatType.Durustluk, 10),
                    "Herkesi kurtaramazsın, önce kendine bak.", E(StatType.Cikar, 10), E(StatType.Durustluk, -5));
            }),
            CreateOrLoad("Merhamet_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "Sana bir sır vereceğim... Ben de yardım istemekten korkuyorum."),
                    (true,  "Yardım istemek zayıflık değil."));
                Require(d, StatType.Durustluk, 60, 100);
                Answers(d,
                    "Sana her zaman yardım ederim.", E(StatType.Durustluk, 5), E(StatType.Ofke, -5),
                    "Korkuların seni sen yapıyor.", E(StatType.Durustluk, 10));
            }),
        };

        // ---- Utangaç (özel stat: Dürüstlük) ----
        lib.positivesByCharacter["Utangac"] = new List<DialogueData>
        {
            CreateOrLoad("Utangac_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "M-merhaba... Benimle mi konuşmak istedin?"),
                    (true,  "Evet, seninle. Acele etme, dinliyorum."),
                    (false, "Kimse daha önce beklememişti..."));
                Require(d, StatType.Durustluk, 50, 100);
                Require(d, StatType.Cikar, 0, 50);
                Answers(d,
                    "Sessizliğin de bir dili var.", E(StatType.Durustluk, 10),
                    "Konuşmazsan kimse seni duymaz ama.", E(StatType.Ofke, 5), E(StatType.Durustluk, -5));
            }),
            CreateOrLoad("Utangac_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "Bugün... bugün sana kendim gelmek istedim."),
                    (true,  "Bu büyük bir adım, farkında mısın?"));
                Require(d, StatType.Durustluk, 55, 100);
                Require(d, StatType.Ofke, 0, 50);
                Answers(d,
                    "Seninle gurur duyuyorum.", E(StatType.Durustluk, 5), E(StatType.Ofke, -5),
                    "Gördün mü, o kadar da zor değilmiş.", E(StatType.Durustluk, 10));
            }),
        };

        // ---- Heyecan (özel stat: Öfke) ----
        lib.positivesByCharacter["Heyecan"] = new List<DialogueData>
        {
            CreateOrLoad("Heyecan_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "Bugün her şey olabilir! Hissediyor musun?!"),
                    (true,  "Enerjin bulaşıcı, itiraf edeyim."),
                    (false, "İşte böyle biriyle konuşmak istiyordum!"));
                Require(d, StatType.Ofke, 35, 80);
                Answers(d,
                    "Hadi bir şeyler yapalım, hemen!", E(StatType.Ofke, 10),
                    "Enerjini biriktir, doğru an gelecek.", E(StatType.Ofke, -5), E(StatType.Cikar, 5));
            }),
            CreateOrLoad("Heyecan_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "Ya bazen bu heyecan beni yoruyor..."),
                    (true,  "Durup dinlenmek de maceranın parçası."));
                Require(d, StatType.Ofke, 30, 70);
                Require(d, StatType.Durustluk, 40, 100);
                Answers(d,
                    "Yorulmak insanca bir şey.", E(StatType.Durustluk, 5), E(StatType.Ofke, -5),
                    "Heyecanın sensiz de sürer, bırak kendini.", E(StatType.Ofke, -10));
            }),
        };

        // ---- Haz (özel stat: Çıkar) ----
        lib.positivesByCharacter["Haz"] = new List<DialogueData>
        {
            CreateOrLoad("Haz_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "Hayat kısa. Neden her anın tadını çıkarmayalım?"),
                    (true,  "Tadını çıkarmakla kaçmak arasında ince bir çizgi var."),
                    (false, "Hmm... Sen ilginç birisin. Devam et."));
                Require(d, StatType.Cikar, 50, 100);
                Answers(d,
                    "Anı yaşa, yarını düşünme.", E(StatType.Cikar, 10),
                    "Gerçek haz, hak edilmiş olandır.", E(StatType.Cikar, -10), E(StatType.Durustluk, 5));
            }),
            CreateOrLoad("Haz_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "İtiraf edeyim... hiçbir şey artık eskisi kadar tatmin etmiyor."),
                    (true,  "Belki aradığın şey dışarıda değildir."));
                Require(d, StatType.Cikar, 45, 90);
                Require(d, StatType.Durustluk, 35, 100);
                Answers(d,
                    "Boşluğu doldurmak için daha fazlası gerekmez.", E(StatType.Cikar, -10),
                    "Yenisini dene, belki o doldurur.", E(StatType.Cikar, 5), E(StatType.Ofke, 5));
            }),
        };

        // ---- Açgözlü (özel stat: Çıkar) ----
        lib.positivesByCharacter["Acgozlu"] = new List<DialogueData>
        {
            CreateOrLoad("Acgozlu_Olumlu_1", d =>
            {
                AddLines(d,
                    (false, "Sende bir şey var... Bana ne kazandırabilirsin?"),
                    (true,  "Her şeyin bir bedeli olmak zorunda mı?"),
                    (false, "Heh... Pazarlık etmeyi biliyorsun. Hoşuma gitti."));
                Require(d, StatType.Cikar, 60, 100);
                Require(d, StatType.Durustluk, 0, 45);
                Answers(d,
                    "Kazan-kazan, ortak olalım.", E(StatType.Cikar, 10),
                    "Bazı şeyler satılık değildir.", E(StatType.Cikar, -10), E(StatType.Durustluk, 10));
            }),
            CreateOrLoad("Acgozlu_Olumlu_2", d =>
            {
                AddLines(d,
                    (false, "Topladığım her şey... bir gün elimden kayarsa diye uyuyamıyorum."),
                    (true,  "Sahip olduklarının sana sahip olmasına izin verme."));
                Require(d, StatType.Cikar, 55, 100);
                Answers(d,
                    "Vermeyi denedin mi hiç?", E(StatType.Cikar, -10), E(StatType.Durustluk, 5),
                    "O zaman daha sıkı tut.", E(StatType.Cikar, 5), E(StatType.Ofke, 5));
            }),
        };

        // ---- Olumsuz havuz (koşulsuz, cevapsız, etkisiz) ----
        string[] negativeLines =
        {
            "Seninle konuşacak havamda değilim.",
            "Şu an sana ayıracak vaktim yok.",
            "...Git başımdan.",
            "Bana uzak dur, olur mu?",
            "Hislerin bana hiç uymuyor.",
            "Sen... hayır. Şimdi olmaz."
        };
        for (int i = 0; i < negativeLines.Length; i++)
        {
            string text = negativeLines[i];
            lib.negatives.Add(CreateOrLoad("Olumsuz_" + (i + 1), d =>
            {
                d.isNegative = true;
                AddLines(d, (false, text));
            }));
        }

        // ---- Kapı ----
        lib.door = CreateOrLoad("Kapi_Kilitli", d =>
        {
            d.isNegative = true;
            AddLines(d,
                (true, "Kapı kilitli..."),
                (true, "Önce diğer karakterlerle konuşmalısın."));
        });

        return lib;
    }

    // ---------- yardımcılar ----------

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(DialogueFolder))
            AssetDatabase.CreateFolder("Assets", "Dialogues");
    }

    static DialogueData CreateOrLoad(string assetName, System.Action<DialogueData> configure)
    {
        string path = DialogueFolder + "/" + assetName + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<DialogueData>();
        configure(data);
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    static void AddLines(DialogueData d, params (bool isLeft, string text)[] lines)
    {
        foreach (var (isLeft, text) in lines)
            d.lines.Add(new DialogueLine { isLeftSide = isLeft, text = text });
    }

    static void Require(DialogueData d, StatType stat, float min, float max)
    {
        d.requirements.Add(new StatRequirement { stat = stat, min = min, max = max });
    }

    static StatEffect E(StatType stat, float amount) => new StatEffect { stat = stat, amount = amount };

    static void Answers(DialogueData d, string textA, params object[] rest)
    {
        // rest: answerA etkileri..., sonra string answerB, sonra answerB etkileri...
        d.answerA = new DialogueAnswer { answerText = textA };
        d.answerB = new DialogueAnswer();
        bool onSecond = false;
        foreach (var item in rest)
        {
            if (item is string s) { d.answerB.answerText = s; onSecond = true; }
            else if (item is StatEffect e)
            {
                if (onSecond) d.answerB.effects.Add(e);
                else d.answerA.effects.Add(e);
            }
        }
    }
}
