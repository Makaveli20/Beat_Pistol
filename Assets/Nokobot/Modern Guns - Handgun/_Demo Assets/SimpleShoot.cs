using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu("Nokobot/Modern Guns/Simple Shoot")]
public class SimpleShoot : MonoBehaviour
{
    [Header("Prefab References")]
    public GameObject muzzleFlashPrefab;

    [Header("Location References")]
    [SerializeField] private Animator gunAnimator;
    [SerializeField] private Transform barrelLocation;

    [Header("Settings")]
    [Tooltip("Specify time to destroy the muzzle flash object")]
    [SerializeField] private float destroyTimer = 2f;

    public InputActionProperty shooting;
    public LineRenderer lineRenderer; // Reference to the LineRenderer component
    public AudioSource shootingAudioSource;
    private int score = 0; // Player's score

    void Start()
    {
        if (barrelLocation == null)
            barrelLocation = transform;

        if (gunAnimator == null)
            gunAnimator = GetComponentInChildren<Animator>();

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        if (shootingAudioSource == null)
            shootingAudioSource = GetComponent<AudioSource>();
        // Initialize the LineRenderer
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
    }

    void Update()
    {
        if (shooting.action.WasPressedThisFrame())
        {
            Shoot();
        }

        // Update the LineRenderer position to always show the raycast line
        UpdateLineRenderer();
    }

    void Shoot()
    {
        // Muzzle flash effect
        if (muzzleFlashPrefab)
        {
            GameObject tempFlash = Instantiate(muzzleFlashPrefab, barrelLocation.position, barrelLocation.rotation);
            Destroy(tempFlash, destroyTimer);
        }

        // Raycast to detect hits
        RaycastHit hit;
        if (Physics.Raycast(barrelLocation.position, barrelLocation.forward, out hit))
        {
            if (hit.collider.CompareTag("Target"))
            {
                Destroy(hit.collider.gameObject); // Destroy the target
                AddScore(10); // Add points to the score
            }
        }
        if (shootingAudioSource)
        {
            shootingAudioSource.Play();
        }
    }

    void UpdateLineRenderer()
    {
        // Set the start position of the line to the barrel location
        lineRenderer.SetPosition(0, barrelLocation.position);

        // Set the end position of the line to a point far forward from the barrel
        lineRenderer.SetPosition(1, barrelLocation.position + barrelLocation.forward * 100f);
    }

    public void AddScore(int points)
    {
        score += points;
        Debug.Log("Score: " + score);
    }
}
