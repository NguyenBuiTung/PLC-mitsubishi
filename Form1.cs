using System;
using System.Windows.Forms;
using ActUtlTypeLib; // Thư viện cần thiết để làm việc với PLC Mitsubishi

namespace Connect_With_PLC_Mitsubishi
{
    public partial class Form1 : Form
    {
        public ActUtlType plc = new ActUtlType(); // Đối tượng ActUtlType để giao tiếp với PLC Mitsubishi

        public Form1()
        {
            InitializeComponent(); // Khởi tạo các thành phần giao diện người dùng
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Đây là sự kiện tải form. Bạn có thể thêm mã khởi tạo ở đây nếu cần.
        }
        private bool IsConnected()
        {
            int testValue;
            int result = plc.GetDevice("D0", out testValue); // Đọc dữ liệu từ địa chỉ "D0" để kiểm tra kết nối

            // Nếu phương thức GetDevice trả về 0, kết nối còn hoạt động; ngược lại, kết nối đã bị ngắt
            return result == 0;
        }

        private void btConnect_Click(object sender, EventArgs e)
        {
            plc.ActLogicalStationNumber = 2; // Thiết lập số trạm logic của PLC (ví dụ: 2)
            short connectResult = (short)plc.Open(); // Mở kết nối với PLC
            if (connectResult == 0)
            {
                MessageBox.Show("Connect with PLC Successful", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Connect with PLC Error", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btDisconnect_Click(object sender, EventArgs e)
        {
            if (IsConnected())
            {
                plc.Close(); // Đóng kết nối với PLC nếu đang kết nối
                MessageBox.Show("Disconnected from PLC", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnRead_Click(object sender, EventArgs e)
        {
            if (!IsConnected()) // Kiểm tra kết nối trước khi đọc dữ liệu
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int readData = 0; // Biến để lưu dữ liệu đọc được
            int readResult = plc.GetDevice("D0", out readData); // Đọc dữ liệu từ thiết bị PLC, ví dụ từ địa chỉ "D100"
            if (readResult == 0)
            {
                txtData.Text = readData.ToString(); // Hiển thị dữ liệu đọc được trong ô văn bản
                MessageBox.Show("Read Success!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Read Error!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnWrite_Click(object sender, EventArgs e)
        {
            if (!IsConnected()) // Kiểm tra kết nối trước khi ghi dữ liệu
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int writeData;
            if (int.TryParse(txtData.Text, out writeData))
            {
                int writeResult = plc.SetDevice("D10", writeData); // Ghi dữ liệu vào thiết bị PLC, ví dụ vào địa chỉ "D100"
                if (writeResult == 0)
                {
                    MessageBox.Show("Write Success!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Write Error!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Invalid Data!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void ReadFloatBlock(string deviceName, int length)
        {
            if (!IsConnected())
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            short[] buffer = new short[length * 2]; // Mỗi số thực (float) chiếm 2 ô nhớ dạng short
            int result = plc.ReadDeviceBlock2(deviceName, length * 2, out buffer[0]); // Đọc dữ liệu dạng short

            if (result == 0)
            {
                float[] floatData = new float[length];
                for (int i = 0; i < length; i++)
                {
                    int high = buffer[i * 2]; // Phần high của float
                    int low = buffer[i * 2 + 1]; // Phần low của float
                    floatData[i] = ConvertToFloat(high, low); // Chuyển đổi 2 ô nhớ thành 1 giá trị float
                }

                // Hiển thị dữ liệu float (ví dụ: trong TextBox hoặc MessageBox)
                txtData.Text = string.Join(", ", floatData);
                MessageBox.Show("Read Float Success!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Read Float Error!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WriteFloatBlock(string deviceName, float[] values)
        {
            if (!IsConnected())
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            short[] buffer = new short[values.Length * 2]; // Mỗi số thực (float) chiếm 2 ô nhớ dạng short
            for (int i = 0; i < values.Length; i++)
            {
                int[] intData = ConvertFromFloat(values[i]); // Chuyển đổi float thành 2 ô nhớ (int)
                buffer[i * 2] = (short)intData[0]; // Chuyển đổi phần thứ nhất sang short
                buffer[i * 2 + 1] = (short)intData[1]; // Chuyển đổi phần thứ hai sang short
            }

            int result = plc.WriteDeviceBlock2(deviceName, values.Length * 2, ref buffer[0]); // Gọi phương thức với buffer kiểu short

            if (result == 0)
            {
                MessageBox.Show("Write Float Success!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Write Float Error!", "Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private float ConvertToFloat(int high, int low)
        {
            byte[] bytes = new byte[4];
            BitConverter.GetBytes((short)high).CopyTo(bytes, 0); // Chuyển phần high thành byte[]
            BitConverter.GetBytes((short)low).CopyTo(bytes, 2); // Chuyển phần low thành byte[]
            return BitConverter.ToSingle(bytes, 0); // Chuyển byte[] thành float
        }


        private int[] ConvertFromFloat(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            int lowWord = bytes[0] | (bytes[1] << 8);
            int highWord = bytes[2] | (bytes[3] << 8);
            return new int[] { lowWord, highWord };
        }

        private void btnReadFloat_Click(object sender, EventArgs e)
        {
            ReadFloatBlock("D4", 2);
        }

        private void btnWriteFloat_Click(object sender, EventArgs e)
        {
            if (!IsConnected()) // Kiểm tra kết nối trước khi ghi dữ liệu
            {
                MessageBox.Show("PLC is not connected", "Message", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ghi giá trị float 30.3 vào D100
            WriteFloatBlock("D16", new float[] { 12.2f });
        }
    }
}