using UnityEngine;

public class WorldProgressAudio : MonoBehaviour
{
    [Header("Refs")]
    public DovinaAudioManager dovinaAudioManager;
    public AmbientLightManager lightManager;

    [Header("Config")]
    public string category = "gp_changes/world";

    private enum Milestone { None = 0, SomeLight = 33, ModerateLight = 66, FullLight = 99 }
    private Milestone highestPlayed = Milestone.None;

    void Update()
    {
        if (!lightManager || !dovinaAudioManager) return;

        float p = lightManager.litPercent;
        Debug.Log("Lit percent: " + p);

        Milestone target =
            p >= .99f ? Milestone.FullLight :
            p >= .66f ? Milestone.ModerateLight :
            p >= .33f ? Milestone.SomeLight :
            Milestone.None;

        if (target > highestPlayed && target != Milestone.None)
        {
            PlayMilestone(target);
            highestPlayed = target;
        }
    }

    private void PlayMilestone(Milestone m)
    {
        // Map milestone → clip index (01->0, 02->1, 03->2)
        int index =
            m == Milestone.SomeLight ? 0 :
            m == Milestone.ModerateLight ? 1 : 2;

        dovinaAudioManager.PlayPriority(category, 2, index, index);
    }
}
