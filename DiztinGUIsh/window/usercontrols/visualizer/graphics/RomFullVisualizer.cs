﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Diz.Core.model;
using Diz.Core.model.snes;
using Diz.Cpu._65816;

// shows a collection of bank controls, so you can visualize the entire ROM

namespace DiztinGUIsh.window.usercontrols
{
    public partial class RomFullVisualizer : UserControl
    {
        private Project project;
        public Data Data => project?.Data;

        private ControlCollection FormControls => flowLayoutPanel1.Controls;
        public Project Project
        {
            get => project;
            set
            {
                DeleteControls();
                project = value;
                if (project != null)
                    Init();
            }
        }

        private void DeleteControls()
        {
            foreach (var rbv in BankControls.Where(rbv => FormControls.Contains(rbv)))
            {
                FormControls.Remove(rbv);
            }
            BankControls.Clear();
        }

        public RomFullVisualizer()
        {
            InitializeComponent();
        }

        public List<RomBankVisualizer> BankControls = new();

        public void Init()
        {
            Debug.Assert(project != null);

            var snesApi = project.Data.GetSnesApi(); 

            var bankSizeBytes = snesApi.GetBankSize();

            for (var bank = 0; bank < snesApi.GetNumberOfBanks(); bank++)
            {
                var bankOffset = bank * bankSizeBytes;
                var bankName = snesApi.GetBankName(bank);

                var bankControl = new RomBankVisualizer(project, bankOffset, bankSizeBytes, bankName);

                AddNewControl(bankControl);
            }
        }

        private void AddNewControl(RomBankVisualizer bankControl)
        {
            BankControls.Add(bankControl);
            flowLayoutPanel1.Controls.Add(bankControl);
        }
    }
}
