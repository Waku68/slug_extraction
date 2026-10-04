using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Marks something drawn on a slug's body that does not hang under the slug's own transform (a held tool and its
    /// arms, a stowed tool, what is in the hands): which slug it belongs to, so things like the slug's own portrait can
    /// find all of it. <see cref="Mark"/> also puts it on the body tools' layer.
    /// </summary>
    public class SlugBodyPart : MonoBehaviour
    {
        /// <summary>Every enabled part, of every slug.</summary>
        public static readonly List<SlugBodyPart> All = new List<SlugBodyPart>();
        /// <summary>The slug (its SlugPop) it belongs to.</summary>
        public Transform owner;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Puts <paramref name="go"/> (and everything under it) on the body tools' layer, as a part of <paramref name="owner"/>'s body.</summary>
        public static void Mark(GameObject go, Transform owner)
        {
            foreach (var c in go.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = SlugToolHolder.BodyToolLayer;
            var part = go.GetComponent<SlugBodyPart>();
            if (!part) part = go.AddComponent<SlugBodyPart>();
            part.owner = owner;
        }
    }
}
