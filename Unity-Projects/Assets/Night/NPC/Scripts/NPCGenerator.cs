using UnityEngine;
using System.IO;

public class NPCGenerator : MonoBehaviour
{
    public Sprite[] faces;

    string[] names;

    void Awake()
    {
        TextAsset txt = Resources.Load<TextAsset>("NPCNames");
        names = txt.text.Split('\n');
    }

    public NPCData Generate()
    {
        NPCData npc = new NPCData();

        npc.npcName = names[Random.Range(0, names.Length)].Trim();

        npc.hp = Random.Range(80, 201);

        npc.atk = Random.Range(5, 31);

        npc.def = Random.Range(2, 21);

        npc.speed = Random.Range(1, 16);

        npc.face = faces[Random.Range(0, faces.Length)];

        return npc;
    }
}