using DG.Tweening;
using PathCreation;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class PigMovement : MonoBehaviour
{
    public PathCreator pathCreator;
    public float speed = 5f;

    [Header("Dinamik Dönüş Ayarları (Hıza Orantılı)")]
    public float lookAheadMultiplier = 0.3f;
    public float turnSpeedMultiplier = 3f;
    public float maxTurnSpeed = 12f;

    [Header("Dönüş (Viraj) Ayarları")]
    public float lookAheadDistance = 1.5f;
    public float turnSpeed = 15f;

    private float distanceTravelled;
    private bool isMoving = false;

    [Header("Tıklama Ayarları")]
    private static float lastClickTime = 0f;
    public float clickCooldown = 0.3f;

    [Header("Bekleme Odası Durumu")]
    public bool isInWaitingRoom = false;
    private Transform targetSlot;
    private float pathLength;

    private Vector3 originalScale;

    void Start ()
    {
        originalScale = transform.localScale;

        if (LevelManager.Instance != null && LevelManager.Instance.pathCreator != null)
        {
            pathCreator = LevelManager.Instance.pathCreator; // Garantilemek için eklendi
            pathLength = LevelManager.Instance.pathCreator.path.length;
        }
    }

    void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                PigMovement hitPig = hit.collider.GetComponentInParent<PigMovement>();

                if (hitPig == this)
                {
                    if (isMoving) return;

                    bool yolaCikabilirMi = false;

                    if (!isMoving && !isInWaitingRoom)
                    {
                        if (Time.time - lastClickTime >= clickCooldown &&
                            (DockManager.Instance == null || DockManager.Instance.IsPigClickable(gameObject)))
                        {
                            yolaCikabilirMi = true;
                            lastClickTime = Time.time;
                            if (DockManager.Instance != null) DockManager.Instance.PigLeftDock(gameObject);
                        }
                    }
                    else if (!isMoving && isInWaitingRoom)
                    {
                        if (Time.time - lastClickTime >= clickCooldown)
                        {
                            yolaCikabilirMi = true;
                            lastClickTime = Time.time;
                            DockManager.Instance.RemoveFromWaitingSlot(gameObject);
                            isInWaitingRoom = false;
                        }
                    }

                    transform.DOKill();

                    if (yolaCikabilirMi)
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.Play2D(0);

                        transform.DOScale(originalScale * 0.75f, 0.2f).SetEase(Ease.OutQuad);
                        distanceTravelled = 0;
                        isMoving = true;
                        GetComponentInChildren<PigShooter>().isShooting = true;
                    }
                    else
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.Play2D(3);

                        transform.localScale = originalScale;
                        transform.DOScale(originalScale * 0.7f, 0.15f).SetEase(Ease.OutQuad).OnComplete(() => {
                            transform.DOScale(originalScale, 0.2f).SetEase(Ease.OutBounce);

                            if (PlayerPrefs.GetInt("Vibration", 1) == 1)
                            {
#if UNITY_EDITOR
                                Debug.Log("<color=yellow><b>[VIBRATION SIMULATION]:</b> ZIIIIIIIIT!</color>");
#elif UNITY_ANDROID || UNITY_IOS
                                Handheld.Vibrate();
#endif
                            }
                        });
                    }
                }
            }
        }

        if (isMoving && pathCreator != null)
        {
            bool isEndgame = false;
            if (LevelManager.Instance != null) isEndgame = LevelManager.Instance.IsEndgameActive();

            float activeSpeed = isEndgame ? speed * 2.5f : speed;
            distanceTravelled += activeSpeed * Time.deltaTime;

            if (distanceTravelled >= pathLength)
            {
                if (isEndgame)
                {
                    distanceTravelled -= pathLength;
                }
                else
                {
                    isMoving = false;
                    GetComponentInChildren<PigShooter>().isShooting = false;
                    targetSlot = DockManager.Instance.TryGetWaitingSlot(gameObject);

                    if (targetSlot != null)
                    {
                        isInWaitingRoom = true;
                        transform.DOKill();
                        transform.DOScale(originalScale, 0.4f).SetEase(Ease.OutBounce);
                    }
                    else Debug.LogError("GAME OVER! Bekleme odasında yer kalmadı!");
                }
            }
            else
            {
                // SENİN ORİJİNAL ÇALIŞAN ROTASYON KODUN
                transform.position = pathCreator.path.GetPointAtDistance(distanceTravelled);
                float dynamicLookAhead = activeSpeed * 0.1f;

                Vector3 pointAhead = pathCreator.path.GetPointAtDistance(distanceTravelled + dynamicLookAhead);
                Vector3 direction = pointAhead - transform.position;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction);
                    float dynamicTurnSpeed = Mathf.Clamp(activeSpeed * 2f, 5f, 20f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * dynamicTurnSpeed);
                }
            }
        }
        else if (isInWaitingRoom && targetSlot != null)
        {
            transform.position = Vector3.Lerp(transform.position, targetSlot.position, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetSlot.rotation, Time.deltaTime * 10f);
        }
    }

    public void ResetPigState ()
    {
        isMoving = false;
        isInWaitingRoom = false;
        distanceTravelled = 0f;
        targetSlot = null;
        transform.DOKill();
        transform.localScale = Vector3.one;
        if (originalScale == Vector3.zero) originalScale = Vector3.one;

        // Havuzdan çıkarken rotayı kaybetmesin
        if (LevelManager.Instance != null && LevelManager.Instance.pathCreator != null)
        {
            pathCreator = LevelManager.Instance.pathCreator;
            pathLength = pathCreator.path.length;
        }
    }
}