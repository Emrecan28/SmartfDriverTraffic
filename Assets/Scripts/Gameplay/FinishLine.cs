using UnityEngine;
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class FinishLine : MonoBehaviour
{
    [SerializeField] private string playerObjectName = "player";
    [SerializeField] private bool advanceLevel = true;
    [SerializeField] private bool openWinPanel = true;

    private bool _triggered;

    private void OnValidate()
    {
        Collider2D c = GetComponent<Collider2D>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other == null) return;

        GameObject hitGo = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        if (!IsPlayer(hitGo, other)) return;

        _triggered = true;
        int completedLevel = LevelProgression.GetCurrentLevel();

        TopDownStepMove2D step = hitGo.GetComponentInParent<TopDownStepMove2D>();
        if (step != null) step.enabled = false;

        if (openWinPanel)
        {
            WinPanelController win = FindFirstObjectByType<WinPanelController>(FindObjectsInactive.Include);
            if (win != null) win.Open();
        }

        if (advanceLevel)
        {
            LevelProgression.MarkCompleted(completedLevel);
            int next = LevelProgression.GetCurrentLevel() + 1;
            LevelProgression.UnlockLevel(next);
        }
    }

    private bool IsPlayer(GameObject go, Collider2D hitCollider)
    {
        if (go == null) return false;

        if (!string.IsNullOrWhiteSpace(playerObjectName))
        {
            Transform t = go.transform;
            if (t.name == playerObjectName) return true;
            if (t.root != null && t.root.name == playerObjectName) return true;
        }

        if (go.GetComponentInParent<TopDownStepMove2D>() != null) return true;
        if (hitCollider != null && hitCollider.GetComponentInParent<TopDownStepMove2D>() != null) return true;

        return false;
    }
}

