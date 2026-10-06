using System.Collections;
using TMPro;
using UnityEngine;

// Secuencia de la escena final: dialogo de Clippy -> Clippy gira y desaparece -> creditos -> imagenes finales.
public class EndingSequence : MonoBehaviour
{
    private const string ClippyDialogueId = "EndingClippy";

    [Header("Clippy")]
    [SerializeField] private DialogBoxController clippyDialog;
    [SerializeField] private SpinSprite clippySpin;
    [SerializeField] private float delayBeforeDialogue = 1f;
    [Tooltip("Dialogo de Clippy. Cada elemento es una linea de la burbuja.")]
    [TextArea(2, 6)]
    [SerializeField] private string[] clippyLines;

    [Header("Creditos")]
    [Tooltip("Panel de creditos (imagen de fondo + titulo + texto de seccion). Se muestra cuando Clippy desaparece.")]
    [SerializeField] private GameObject creditsRoot;
    [SerializeField] private TMP_Text sectionText;
    [SerializeField] private AudioClip creditsMusic;
    [Tooltip("Secciones de los creditos. Se muestran una despues de la otra.")]
    [TextArea(3, 10)]
    [SerializeField] private string[] creditSections;
    [SerializeField] private float secondsPerSection = 5f;
    [SerializeField] private float fadeSeconds = 1f;

    [Header("Imagen final")]
    [Tooltip("Espera entre el ultimo texto de los creditos y la imagen final.")]
    [SerializeField] private float secondsBeforeFinalImage = 10f;
    [SerializeField] private GameObject finalImage;

    private bool clippyDialogueFinished;

    private void Awake()
    {
        if (creditsRoot != null)
        {
            creditsRoot.SetActive(false);
        }

        if (finalImage != null)
        {
            finalImage.SetActive(false);
        }
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delayBeforeDialogue);

        yield return PlayClippyDialogue();
        yield return PlayClippySpin();
        yield return PlayCredits();

        yield return new WaitForSeconds(secondsBeforeFinalImage);

        if (finalImage != null)
        {
            finalImage.SetActive(true);
        }
    }

    private IEnumerator PlayClippyDialogue()
    {
        if (clippyDialog == null || clippyLines == null || clippyLines.Length == 0)
        {
            yield break;
        }

        clippyDialogueFinished = false;
        clippyDialog.DialogueFinished += OnDialogueFinished;
        clippyDialog.ShowDialogue(ClippyDialogueId, clippyLines);

        yield return new WaitUntil(() => clippyDialogueFinished);

        clippyDialog.DialogueFinished -= OnDialogueFinished;
    }

    private void OnDialogueFinished(string dialogueId)
    {
        if (dialogueId == ClippyDialogueId)
        {
            clippyDialogueFinished = true;
        }
    }

    // SpinSprite desactiva a Clippy cuando termina de girar (con 'Disappear While Spin' activo).
    private IEnumerator PlayClippySpin()
    {
        if (clippySpin == null)
        {
            yield break;
        }

        clippySpin.ActivateSpin();
        yield return new WaitWhile(() => clippySpin.gameObject.activeInHierarchy);
    }

    private IEnumerator PlayCredits()
    {
        if (creditsRoot != null)
        {
            creditsRoot.SetActive(true);
        }

        if (MusicManager.Instance != null && creditsMusic != null)
        {
            MusicManager.Instance.PlayTrack(creditsMusic);
        }

        if (sectionText == null || creditSections == null)
        {
            yield break;
        }

        sectionText.alpha = 0f;

        for (int i = 0; i < creditSections.Length; i++)
        {
            sectionText.text = creditSections[i] ?? string.Empty;

            yield return FadeSection(0f, 1f);
            yield return new WaitForSeconds(secondsPerSection);
            yield return FadeSection(1f, 0f);
        }

        sectionText.text = string.Empty;
    }

    private IEnumerator FadeSection(float from, float to)
    {
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            sectionText.alpha = Mathf.Lerp(from, to, t / fadeSeconds);
            yield return null;
        }

        sectionText.alpha = to;
    }
}
