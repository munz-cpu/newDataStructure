using UnityEngine;

public sealed class 왕스킬아이콘목록 : ScriptableObject
{
    [SerializeField] Sprite[] 아이콘들;
    static 왕스킬아이콘목록 목록;

    public static Sprite 가져오기(장착기술 기술)
    {
        if (목록 == null)
            목록 = Resources.Load<왕스킬아이콘목록>("Classes/SkillIcons");
        int 번호 = (int)기술;
        return 목록 != null && (uint)번호 < 목록.아이콘들.Length ? 목록.아이콘들[번호] : null;
    }
}
