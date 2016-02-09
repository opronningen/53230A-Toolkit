using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace _53230A {

    // Enumeration of all (most - ROSC is currently missing) valid SCPI abbreviations
    public enum SettingID {
        func,
        calc_aver,
        calc_lim_low,
        calc_lim,
        calc_lim_upp,
        calc_scal_func,
        calc_scal_gain,
        calc_scal_inv,
        calc_scal_offs,
        calc_scal_ref,
        calc_scal_ref_auto,
        calc_scal,
        calc_scal_unit,
        calc_scal_unit_stat,
        calc_smo_resp,
        calc_smo,
        calc,
        calc2_tran_hist_poin,
        calc2_tran_hist_rang_low,
        calc2_tran_hist_rang_upp,
        calc2_tran_hist_rang_auto_coun,
        calc2_tran_hist_rang_auto,
        calc2_tran_hist,
        cal_val,
        data_poin_even_thr,
        form_bord,
        form,
        hcop_sdum_data_form,
        inp_coup,
        inp_filt,
        inp_imp,
        inp_lev,
        inp_lev_rel,
        inp_lev2,
        inp_lev2_rel,
        inp_lev_auto,
        inp_nrej,
        inp_prob,
        inp_rang,
        inp_slop,
        inp_slop2,
        inp2_coup,
        inp2_filt,
        inp2_imp,
        inp2_lev,
        inp2_lev_rel,
        inp2_lev2,
        inp2_lev2_rel,
        inp2_lev_auto,
        inp2_nrej,
        inp2_prob,
        inp2_rang,
        inp2_slop,
        inp2_slop2,
        inp3_burs_lev,
        mmem_cdir,
        outp_pol,
        outp,
        samp_coun,
        freq_burs_gate_auto,
        freq_burs_gate_del,
        freq_burs_gate_narr,
        freq_burs_gate_time,
        freq_gate_pol,
        freq_gate_sour,
        freq_gate_time,
        freq_mode,
        gate_ext_sour,
        gate_star_del_even,
        gate_star_del_sour,
        gate_star_del_time,
        gate_star_slop,
        gate_star_sour,
        gate_stop_hold_even,
        gate_stop_hold_sour,
        gate_stop_hold_time,
        gate_stop_slop,
        gate_stop_sour,
        tint_gate_pol,
        tint_gate_sour,
        tot_gate_pol,
        tot_gate_sour,
        tot_gate_time,
        tst_rate,
        trig_coun,
        trig_del,
        trig_slop,
        trig_sour
    };

    // Base class for settings
    public class Setting {
        public SettingID ID;
        public string DisplayName;      // Text to show as a label with a control in the GUI
        public string SCPI;             // Text to send to instrument
        public string ToolTip;          // Text to show as tooltip to a control in the GUI

        // Flag to signify if the setting has been changed
        public bool changed = false;

        // Determines if the setting is active or not - i.e. another setting may result in this setting
        // being ignored by the instrument.
        // This can be overridden by the various settings to reflect dependencies between the settings.
        public Func<bool> IsActive = delegate () { return true; };
    }

    // A setting that takes a numeric value
    public class NumericSetting : Setting {
        public double MaxValue;
        public double MinValue;

        private double _value;
        public double value
        {
            get { return _value; }
            set
            {
                if (_value != value) {

                    if (value > MaxValue || value < MinValue)
                        throw new System.ArgumentOutOfRangeException();

                    _value = value;
                    changed = true;
                }
            }
        }

        public override string ToString() {
            return (SCPI + " " + _value);
        }
    }

    // A setting that can take a defined list of values
    public class ListSetting : Setting {
        public string[] AllowableValues = { "" };

        private int _SelectedIndex = 0;
        public int SelectedIndex
        {
            get { return _SelectedIndex; }
            set
            {
                if (value < 0 || value > AllowableValues.Length)
                    throw new System.ArgumentOutOfRangeException();

                _SelectedIndex = value;
            }
        }

        public string SelectedItem() {
            if (AllowableValues == null)
                return null;

            return (AllowableValues[_SelectedIndex]);
        }

        public override string ToString() {
            return (SCPI + " " + SelectedItem());
        }
    }

    public class Configuration : List<Setting> {

        // Return a setting based on its associated SCPI string
        public Setting GetBySCPI(string scpi) {
            return this.FirstOrDefault(s => s.SCPI.Equals(scpi));
        }

        public Setting GetByID(SettingID id) {
            return this.FirstOrDefault(s => s.ID == id);
        }

        public NumericSetting GetNumericByID(SettingID id) {
            return this.FirstOrDefault(s => s.ID == id) as NumericSetting;
        }

        public ListSetting GetListByID(SettingID id) {
            return this.FirstOrDefault(s => s.ID == id) as ListSetting;
        }

        // Clear all changed flags
        public void ClearChangedFlags() {
            this.ForEach(s => s.changed = false);
        }

        // Returns the config-statements that has been changed
        public List<string> BuildDeltaConfig() {
            List<string> l = new List<string>();
            this.FindAll(s => s.changed).ForEach(s => l.Add(s.ToString()));
            return l;
        }

        // Return a complete list of configuration-statements
        public List<string> BuildCompleteConfig() {
            List<string> l = new List<string>();
            this.ForEach(s => l.Add(s.ToString()));
            return l;
        }

        // Read current configuration from the instrument
        public void LearnConfig(string state) {
            string tmp;
            string[] key_val = new string[2];

            string[] settings = state.Split(';');

            foreach(string str in settings) {
                tmp = str.Trim();
                key_val = tmp.Split(new char[] { ' ' }, 2);

                Setting s = GetBySCPI(key_val[0]);
                if (s == null)
                    continue;

                if(s is ListSetting) {
                    ListSetting l = s as ListSetting;
                    int index = Array.IndexOf(l.AllowableValues, key_val[1]);
                    if (index == -1)
                        throw new ArgumentException("Error! Value \'" + key_val[1] + "\' is not allowed for setting \'" + key_val[0] + "\'");

                    l.SelectedIndex = index;
                    
                } else if(s is NumericSetting){
                    NumericSetting n = s as NumericSetting;
                    double val = 0;
                    if(! Double.TryParse(key_val[1], NumberStyles.AllowExponent | NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                        throw new ArgumentException("Error! Could not parse numeric value \'" + key_val[1] + "\' for setting \'" + key_val[0] + "\'");

                    n.value = val;
                }
            }

            ClearChangedFlags();
        }

        // Set up the (default) configuration
        public Configuration() {
            //ListSetting s = new ListSetting();
            //s.SCPI = ":INPut1:ATTenuation";
            //s.ToolTip = "Attenuates the input signal with 1 or 10. The attenuation is automatically set if the input level is set to AUTO.";
            //s.AllowableValues = new string[] { "Auto", "1", "10" };
            //s.IsActive = delegate () { return ((ListSetting)this.GetBySCPI(":INPut1:LEVel:AUTO")).SelectedIndex == 1; };
            //this.Add(s);

            // Start with only the relevant settings, add on as we go..

            // Function section

            // Func
            ListSetting l = new ListSetting();
            l.ID = SettingID.func;
            l.DisplayName = "Function";
            l.SCPI = ":FUNC";
            l.ToolTip = "Selects instrument measurement function.";
            l.AllowableValues = new string[] {
                "\"FREQ\"", "\"FREQ 2\"", "\"FREQ 3\"",
                "FREQ:RAT 1,2", "FREQ:RAT 1,3", "FREQ:RAT 1,3", "FREQ:RAT 2,1", "FREQ:RAT 2,3", "FREQ:RAT 3,1","FREQ:RAT 3,2",
                "PER 1", "PER 2", "PER 3",
                "FTI 1", "FTI 2",
                "NDUT 1", "NDUT 2",
                "PDUT 1", "PDUT 2",
                "NWID 1", "NWID 2",
                "PWID 1", "PWID 2",
                "PHA 1,2", "PHA 2,1",
                "RTIM 1", "RTIM 2",
                "SPER 1", "SPER 2",
                "\"TINT\"", "\"TINT 2,1\"", "\"TINT 1\"", "TINT \"2\"",
                "TOT 1", "TOT 2",
                "TST 1", "TST 2","TST 3",
                "FREQ:BURS",           
                "FREQ:PRF", 
                "FREQ:PRI", 
                "NWIDth:BURS", 
                "PWIDth:BURS"
            };
            this.Add(l);

            // Trigger section

            // Trigger source
            l = new ListSetting();
            l.ID = SettingID.trig_sour;
            l.DisplayName = "Trigger source";
            l.SCPI = ":TRIG:SOUR";
            l.ToolTip = "Selects instrument trigger source.";
            l.AllowableValues = new string[] {"IMM", "EXT", "BUS"};
            this.Add(l);

            // Trigger slope
            l = new ListSetting();
            l.ID = SettingID.trig_slop;
            l.DisplayName = "Trigger slope";
            l.SCPI = ":TRIG:SLOP";
            l.ToolTip = "Selects trigger slope, POSitive or NEGative.";
            l.AllowableValues = new string[] { "POS", "NEG" };
            l.IsActive = delegate () { return this.GetListByID(SettingID.trig_sour).SelectedIndex == 2; }; // Only active if ext trigger selected
            this.Add(l);

            // Number of triggers to accept
            NumericSetting n = new NumericSetting();
            n.ID = SettingID.trig_coun;
            n.DisplayName = "Trigger count";
            n.SCPI = ":TRIG:COUN";
            n.ToolTip = "Number of triggers to accept. Total number of Readings = Sample Count x Trigger Count.";
            n.MaxValue = 1e6;
            n.MinValue = 1;
            //n.IsActive = null;  // To do. Ignored if func = freq 1|2|3, and :freq:mode = CONT | RCON
            this.Add(n);

            // Trigger delay
            n = new NumericSetting();
            n.ID = SettingID.trig_del;
            n.DisplayName = "Trigger delay";
            n.SCPI = ":TRIG:DEL";
            n.ToolTip = "Sets the delay time in seconds between the trigger signal and enabling the gate open for the first measurement. This may be useful in applications where the signal you want to measure is delayed with respect to the trigger.";
            n.MaxValue = 3600;
            n.MinValue = 0;
            this.Add(n);

            // Samples per trigger
            n = new NumericSetting();
            n.ID = SettingID.samp_coun;
            n.DisplayName = "Sample count";
            n.SCPI = ":SAMP:COUN";
            n.ToolTip = "Number of samples to take per trigger-event.";
            n.MaxValue = 1e6;
            n.MinValue = 1;
            //n.IsActive = null;  // To do
            this.Add(n);

            // Format
            l = new ListSetting();
            l.ID = SettingID.form;
            l.DisplayName = "Data format";
            l.SCPI = "FORM";
            l.ToolTip = "Defines if data is returned in 15-character printable ascii strings, or 64-bit binary values.";
            l.AllowableValues = new string[] { "ASC,15", "REAL,64" };
            this.Add(l);

            // Big or little endian
            l = new ListSetting();
            l.ID = SettingID.form_bord;
            l.DisplayName = "Byte order";
            l.SCPI = "FORM:BORD";
            l.ToolTip = "Defines if 64-bit binary values are returned big- or little-endian. Intel is little endian, SWAP";
            l.AllowableValues = new string[] { "NORM", "SWAP" };
            n.IsActive = delegate () { return GetListByID(SettingID.form).SelectedIndex == 2; };
            this.Add(l);
        }
    }
}