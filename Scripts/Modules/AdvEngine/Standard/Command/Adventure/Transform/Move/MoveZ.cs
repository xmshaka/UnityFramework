﻿
#if ENABLE_MOONSHARP

using System;
using DG.Tweening;
using Extensions;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class MoveZ : Command
    {
        //----- params -----

        //----- field -----
        
        //----- property -----

        public override string CommandName { get { return "MoveZ"; } }

        //----- method -----

        public override object GetCommandDelegate()
        {
            return (Func<string, float, float, string, bool, DynValue>)CommandFunction;
        }

        private DynValue CommandFunction(string identifier, float position, float duration = 0, string easingType = null, bool wait = true)
        {
            var advEngine = AdvEngine.Instance;

            var advObject = advEngine.ObjectManager.Get<AdvObject>(identifier);

            if (advObject != null)
            {
                TweenCallback onComplete = () =>
                {
                    if (wait)
                    {
                        advEngine.Resume();
                    }
                };

                var ease = EnumExtensions.FindByName(easingType, Ease.Linear);

                var tweener = advObject.transform.DOLocalMoveZ(position, duration)
                    .SetEase(ease)
                    .OnComplete(onComplete);

                advEngine.SetTweenTimeScale(tweener);
            }

            return wait ? YieldWait : DynValue.Nil;
        }
    }
}

#endif
