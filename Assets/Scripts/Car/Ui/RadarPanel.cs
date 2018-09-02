using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.System;
using Assets.System;
using UnityEngine;

namespace Assets.Scripts.Car.UI
{
    public class RadarPanel : Panel
    {
        private const int SpriteFrames = 30;
        private const float RadarSweepFullRotationTime = 5f;
        private const float FrameTime = RadarSweepFullRotationTime / SpriteFrames;
        private const float DegreesPerSweep = 360f / SpriteFrames;
        private const float ShortRangeRadius = 100f;
        private const float LongRangeRadius = 500f;
        private static readonly Vector2Int RadarCenre = new Vector2Int(97, 55);
        private const float RadarTextureRadius = 50f;
        private static readonly Color32 RadarContactNewColour = new Color32(82, 155, 82, 255);
        private static readonly Color32 RadarContactOldColour = new Color32(0, 66, 0, 255);
        private bool _longRange;
        private CarController _target;
        private int _targetIndex;
        private AudioSource _radarAudio;
        private AudioClip _newContact;
        private AudioClip _sweepContact;

        private readonly CarController _car;
        private readonly Dictionary<CarController, RadarContact> _radarContacts;
        private readonly Color32[] _blipPixels;

        private readonly I76Sprite[] _sweepSprites;
        private readonly I76Sprite[] _targetSprites;
        private readonly I76Sprite[] _rangeSprites;
        private readonly I76Sprite[] _ledSprites;

        private class RadarContact
        {
            public float TimeSinceSweep;
            public float Distance;
            public Vector2 Normal;
        }

        private int _radarImageIndex;
        private float _sweepTick;

        public void ToggleRange()
        {
            _longRange = !_longRange;
        }

        public void TargetNearest()
        {
            if (_radarContacts.Count == 0)
            {
                return;
            }

            float nearest = float.MaxValue;
            foreach (KeyValuePair<CarController, RadarContact> contacts in _radarContacts)
            {
                if (contacts.Value.Distance < nearest)
                {
                    _target = contacts.Key;
                    nearest = contacts.Value.Distance;
                }
            }
        }

        public void CycleTarget()
        {
            if (_radarContacts.Count == 0)
            {
                return;
            }

            _targetIndex = ++_targetIndex % _radarContacts.Count;
            _target = _radarContacts.ElementAt(_targetIndex).Key;
        }

        public void ClearTarget()
        {
            _target = null;
        }

        public RadarPanel(CarController car, Transform firstPersonTransform) : base(firstPersonTransform, "RAD", "zrad.map")
        {
            if (ReferenceImage == null)
            {
                return;
            }
            
            _blipPixels = new Color32[4];
            for (int i = 0; i < 4; ++i)
            {
                _blipPixels[i] = Color.yellow;
            }

            _longRange = true;
            _car = car;
            _radarContacts = new Dictionary<CarController, RadarContact>();
            _radarAudio = _car.gameObject.AddComponent<AudioSource>();
            _radarAudio.playOnAwake = false;
            _radarAudio.volume = 0.2f;

            CacheManager cacheManager = Object.FindObjectOfType<CacheManager>();

            _newContact = cacheManager.GetAudioClip("cgrowl.gpw");
            _sweepContact = cacheManager.GetAudioClip("cradar.gpw");

            _rangeSprites = new I76Sprite[3];
            for (int i = 0; i < 3; ++i)
            {
                _rangeSprites[i] = SpriteManager.GetSprite("zrge.map", "range_" + (i + 1));
            }

            _ledSprites = new I76Sprite[2];
            for (int i = 0; i < 2; ++i)
            {
                _ledSprites[i] = SpriteManager.GetSprite("ztge.map", "radled_" + (i + 1));
            }

            _sweepSprites = new I76Sprite[SpriteFrames];
            _targetSprites = new I76Sprite[SpriteFrames];
            for (int i = 0; i < SpriteFrames; ++i)
            {
                string numberString = string.Format("{0:00}", i);
                Texture2D sweepTexture = cacheManager.GetTexture("zradf0" + numberString);
                Texture2D targetTexture = cacheManager.GetTexture("zradb0" + numberString);

                I76Sprite sweepSprite = new I76Sprite
                {
                    Width = sweepTexture.width,
                    Height = sweepTexture.height,
                    Pixels = sweepTexture.GetPixels()
                };

                I76Sprite targetSprite = new I76Sprite
                {
                    Width = targetTexture.width,
                    Height = targetTexture.height,
                    Pixels = targetTexture.GetPixels()
                };

                _sweepSprites[i] = sweepSprite;
                _targetSprites[i] = targetSprite;
            }
        }

        public void Update()
        {
            float radarRange = _longRange ? LongRangeRadius : ShortRangeRadius;
            Vector3 pos = _car.transform.position;

            CarController[] cars = Object.FindObjectsOfType<CarController>();
            int carCount = cars.Length;
            for (int i = 0; i < carCount; ++i)
            {
                CarController car = cars[i];
                if (car == _car)
                {
                    continue;
                }

                Vector3 carPos = car.transform.position;
                float distance = Vector3.Distance(carPos, pos);
                if (distance < radarRange)
                {
                    Vector2 normal;
                    normal.x = pos.x - carPos.x;
                    normal.y = pos.z - carPos.z;
                    normal.Normalize();

                    RadarContact contact;
                    if (!_radarContacts.TryGetValue(car, out contact))
                    {
                        if (!_radarAudio.isPlaying || _radarAudio.clip != _newContact)
                        {
                            _radarAudio.clip = _newContact;
                            _radarAudio.Play();
                        }

                        contact = new RadarContact
                        {
                            TimeSinceSweep = 0f,
                            Distance = distance,
                            Normal = normal
                        };
                        _radarContacts.Add(car, contact);
                    }
                    else
                    {
                        contact.Distance = distance;
                        contact.Normal = normal;
                    }
                }
                else
                {
                    if (_target == car)
                    {
                        _target = null;
                    }

                    _radarContacts.Remove(car);
                }
            }
            
            if (_target == null)
            {
                _sweepTick += Time.deltaTime;
                if (_sweepTick > FrameTime)
                {
                    _radarImageIndex = ++_radarImageIndex % SpriteFrames;
                    ReferenceImage.ApplySprite(null, _sweepSprites[_radarImageIndex], false);
                    _sweepTick -= FrameTime;
                }
            }
            
            Texture2D radarTexture = ReferenceImage.MainTexture;
            float pixelFactor = RadarTextureRadius / radarRange;

            // Process radar contacts.
            float deltaTime = Time.deltaTime;
            foreach (KeyValuePair<CarController, RadarContact> radarContact in _radarContacts)
            {
                RadarContact contact = radarContact.Value;

                // Rotate radar contact points based on car's facing direction.
                contact.Normal = Utils.RotateVector(contact.Normal, _car.transform.eulerAngles.y + 180f);

                Vector2 radarOffset;
                radarOffset.x = RadarCenre.x + contact.Normal.x * contact.Distance * pixelFactor;
                radarOffset.y = RadarCenre.y + contact.Normal.y * contact.Distance * pixelFactor;

                float angle = Vector2.Angle(contact.Normal, Vector2.up);
                angle = Mathf.Sign(Vector3.Cross(contact.Normal, Vector2.up).z) < 0 ? (360 - angle) % 360 : angle;

                int sweepIndex = Mathf.CeilToInt(angle / DegreesPerSweep);

                if (_target == radarContact.Key)
                {
                    ReferenceImage.ApplySprite(null, _targetSprites[sweepIndex], false);
                }

                if (_target == null)
                {
                    if (sweepIndex == _radarImageIndex)
                    {
                        if (!_radarAudio.isPlaying)
                        {
                            _radarAudio.clip = _sweepContact;
                            _radarAudio.Play();
                        }

                        contact.TimeSinceSweep = 0f;
                    }
                    else
                    {
                        contact.TimeSinceSweep += deltaTime;
                    }
                }
                else
                {
                    // When we have a target, just use the same colour for all radar contacts.
                    contact.TimeSinceSweep = 0f;
                }

                // Colours here aren't the same as original game - try and determine colour pattern.
                Color32 radarColour = Color32.Lerp(RadarContactNewColour, RadarContactOldColour, contact.TimeSinceSweep / 5f);
                for (int i = 0; i < 4; ++i)
                {
                    _blipPixels[i] = radarColour;
                }

                radarTexture.SetPixels32((int)radarOffset.x, (int)radarOffset.y, 2, 2, _blipPixels);
            }

            bool hasRadarContacts = _radarContacts.Count > 0;
            ReferenceImage.ApplySprite("range_pos", _rangeSprites[_longRange ? 2 : 0], false);
            ReferenceImage.ApplySprite("led_pos", _ledSprites[hasRadarContacts ? 0 : 1], true);
        }
    }
}
