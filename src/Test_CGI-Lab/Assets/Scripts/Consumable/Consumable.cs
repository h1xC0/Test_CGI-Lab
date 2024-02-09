using System.Collections;
using Characters;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Consumable
{
    /// <summary>
    /// Defines a consumable (called "power up" in game). Each consumable is derived from this and implements its functions.
    /// </summary>
    public abstract class Consumable : MonoBehaviour
    {
        public float duration;

        public enum ConsumableType
        {
            NONE,
            COIN_MAG,
            SCORE_MULTIPLAYER,
            INVINCIBILITY,
            EXTRALIFE,
            MAX_COUNT
        }

        public Sprite icon;
        public AudioClip activatedSound;
        public AssetReference ActivatedParticleReference;
        public bool canBeSpawned = true;

        public bool active {  get { return m_Active; } }
        public float timeActive {  get { return m_SinceStart; } }

        protected bool m_Active = true;
        protected float m_SinceStart;
        protected ParticleSystem m_ParticleSpawned;

        public abstract ConsumableType GetConsumableType();
        public abstract string GetConsumableName();
        public abstract int GetPrice();
        public abstract int GetPremiumCost();

        public void ResetTime()
        {
            m_SinceStart = 0;
        }

        public virtual bool CanBeUsed(CharacterInputController c)
        {
            return true;
        }

        public virtual IEnumerator Started(CharacterInputController c)
        {
            m_SinceStart = 0;

            if (activatedSound != null)
            {
                c.powerupSource.clip = activatedSound;
                c.powerupSource.Play();
            }

            if(ActivatedParticleReference != null)
            {
                var op = ActivatedParticleReference.InstantiateAsync();
                yield return op;
                m_ParticleSpawned = op.Result.GetComponent<ParticleSystem>();
                if (!m_ParticleSpawned.main.loop)
                    StartCoroutine(TimedRelease(m_ParticleSpawned.gameObject, m_ParticleSpawned.main.duration));

                m_ParticleSpawned.transform.SetParent(c.characterCollider.transform);
                m_ParticleSpawned.transform.localPosition = op.Result.transform.position;
            }
        }

        IEnumerator TimedRelease(GameObject obj, float time)
        {
            yield return new WaitForSeconds(time);
            Addressables.ReleaseInstance(obj);
        }

        public virtual void Tick(CharacterInputController c)
        {
            m_SinceStart += Time.deltaTime;
            if (m_SinceStart >= duration)
            {
                m_Active = false;
                return;
            }
        }

        public virtual void Ended(CharacterInputController c)
        {
            if (m_ParticleSpawned != null)
            {
                if (m_ParticleSpawned.main.loop)
                    Addressables.ReleaseInstance(m_ParticleSpawned.gameObject);
            }

            if (activatedSound != null && c.powerupSource.clip == activatedSound)
                c.powerupSource.Stop(); 

            for (int i = 0; i < c.consumables.Count; ++i)
            {
                if (c.consumables[i].active && c.consumables[i].activatedSound != null)
                {
                    c.powerupSource.clip = c.consumables[i].activatedSound;
                    c.powerupSource.Play();
                }
            }
        }
    }
}
