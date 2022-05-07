using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SpecialSelect : MonoBehaviour
{   
    [Header("Selectors")]
    public Button[] selectors;
    [SerializeField] float fillSpeed;
    [SerializeField] Image specialActiveIndicator;

    [Header("Particles")]
    [SerializeField] GameObject spiritParticlePrefab;
    [SerializeField] ParticleSystem[] chargeParticles;
    [SerializeField] ParticleSystem[] chargeLineParticles;

    ParticleSystem[] activeChargeParticles = new ParticleSystem[5];
    bool[] activeSelectors = new bool[5];
    bool[] charging = new bool[5];
    int? selectedButtonInd = null;
    BoardManager boardManager;

    void Start()
    {
        boardManager = GameManager.Instance.boardManager;
        boardManager.specialSelect = this;
        for (int ind = 0; ind < selectors.Length; ind++)
        {
            activeChargeParticles[ind] = Instantiate(chargeParticles[ind], selectors[ind].transform);
            activeChargeParticles[ind].transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        //potential performance issue?
        for (int ind = 0; ind < selectors.Length; ind++)
        {
            Image selector = selectors[ind].GetComponent<Image>();
            selector.enabled = !boardManager.battleManager.partyMembers[ind].knockedOut;
            float spiritLevel = (float) boardManager.spiritLevels[ind] / 100f;
            if (selector.fillAmount < spiritLevel && charging[ind])
            {
                
                selector.fillAmount += fillSpeed * Time.deltaTime;
                chargeLineParticles[ind].transform.localPosition = new Vector3(0f, Mathf.Lerp(-0.4f, 0.4f, selector.fillAmount), -1f);
                if (!chargeLineParticles[ind].isPlaying)
                {
                    chargeLineParticles[ind].Play();
                }
            }
            else if (selector.fillAmount > spiritLevel)
            {
                selector.fillAmount -= fillSpeed * Time.deltaTime;
            }

            selector.fillAmount = Mathf.Clamp(selector.fillAmount, 0, spiritLevel);

            if (selector.fillAmount == spiritLevel)
            {
                if (charging[ind])
                {
                    charging[ind] = false;
                    chargeLineParticles[ind].Stop();
                }
                if (spiritLevel == 1f && !activeSelectors[ind])
                {
                    activeSelectors[ind] = true;
                    activeChargeParticles[ind].Play();
                }
            }
        }
    }

    public void ButtonPress(int buttonInd)
    {
        if (boardManager.canSwap && activeSelectors[buttonInd] && !boardManager.battleManager.partyMembers[buttonInd].knockedOut && selectedButtonInd != buttonInd)
        {
            selectedButtonInd = buttonInd;
            SelectVisual(buttonInd);
            boardManager.ToggleSpecialTargeting(buttonInd);
        }
    }

    void SelectVisual(int buttonInd)
    {
        specialActiveIndicator.transform.localPosition = selectors[buttonInd].transform.localPosition;
        specialActiveIndicator.enabled = true;
    }

    public void CancelSelect()
    {
        selectedButtonInd = null;
        specialActiveIndicator.enabled = false;
        GameManager.Instance.battleManager.ToggleSpecialTargeting(0, true);
    }

    public void OnSpecialActivate(int buttonInd)
    {
        Instantiate(boardManager.activateParticle, selectors[buttonInd].transform.position, Quaternion.identity);
        selectedButtonInd = null;
        specialActiveIndicator.enabled = false;
        activeChargeParticles[buttonInd].Stop();
        activeSelectors[buttonInd] = false;
        // boardManager.ActivatePlayerSpecial(buttonInd);
        boardManager.spiritLevels[buttonInd] = 0;
    }

    public void CreateSpiritParticles(Vector2 spawnPoint, int numParticles, TileColor tileColor, Color particleColor)
    {
        for (int ind = 0; ind < numParticles; ind++)
        {
            Vector3 startPoint = new Vector3(spawnPoint.x, spawnPoint.y, -2f);
            GameObject particle = Instantiate(spiritParticlePrefab, startPoint, Quaternion.identity);
            particle.GetComponent<SpriteRenderer>().color = particleColor;
            Transform particleTransform = particle.transform;
            particleTransform.localScale = Vector3.zero;
            float destX = particleTransform.localPosition.x + 2f * Mathf.Cos(2f * Mathf.PI * ((float)ind / numParticles));
            float destY = particleTransform.localPosition.y + 2f * Mathf.Sin(2f * Mathf.PI * ((float)ind / numParticles));
            var sequence = DOTween.Sequence();
            sequence.Append(particleTransform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
            var initialExpand = particleTransform.DOLocalMove(new Vector3(destX, destY, -2f), 0.5f);
            initialExpand.SetEase(Ease.OutQuad);
            sequence.Join(initialExpand);
            var goToGem = particleTransform.DOMove(selectors[(int)tileColor].gameObject.transform.position, 0.5f);
            goToGem.SetEase(Ease.InBack);
            goToGem.OnComplete(() => {
                charging[(int)tileColor] = true;
            });
            sequence.Append(goToGem);
            sequence.Append(particleTransform.DOScale(Vector3.zero, 0.35f));
            sequence.OnComplete(() => Destroy(particle));
        }
    }
}
