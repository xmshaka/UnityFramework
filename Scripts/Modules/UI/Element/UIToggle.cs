﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Toggle))]
    public abstract class UIToggle : UIElement<Toggle>
    {
        //----- params -----

        //----- field -----

        private Subject<bool> onChange = null;

        //----- property -----

        public Toggle Toggle { get { return component; } }

        public bool isOn
        {
            get { return component.isOn; }
            set { component.isOn = value; }
        }

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

            Toggle.onValueChanged.AddListener(OnToggleValueChanged);
        }

        public override void Modify(){ }

        private void OnToggleValueChanged(bool value)
        {
            if (onChange != null)
            {
                onChange.OnNext(value);
            }
        }

        public IObservable<bool> OnChangeAsObservable()
        {
            return onChange ?? (onChange = new Subject<bool>());
        }
    }
}