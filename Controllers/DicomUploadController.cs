using FellowOakDicom;
using FellowOakDicom.Network.Client;
using FellowOakDicom.Network;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.Controllers
{
    [Route("api/dicom")]
    [ApiController]
    public class DicomUploadController : ControllerBase
    {
        
        [HttpPost("upload")]
        public async Task<IActionResult> UploadDICOMFile(IFormFile file)
        {
            if (file == null || !file.FileName.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Invalid file. Only .dcm files are allowed.");

            try
            {
                using var stream = file.OpenReadStream();
                var dicomFile = DicomFile.Open(stream);
                var patientId = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.PatientID, "");
                var studyDescription = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.StudyDescription, "");
                var modality = dicomFile.Dataset.GetSingleValueOrDefault(DicomTag.Modality, "");
                if (string.IsNullOrWhiteSpace(patientId))
                    return BadRequest("Missing PatientID.");
                if (string.IsNullOrWhiteSpace(studyDescription))
                    return BadRequest("Missing studyDescription.");
                if (string.IsNullOrWhiteSpace(modality))
                    return BadRequest("Missing modality.");
                return Ok("File uploaded successfully.");
            }
            catch
            {
                return BadRequest("Invalid DICOM file.");
            }
        }

        [HttpPost("store")]
       
        public async Task<IActionResult> SendDicomFile(IFormFile file)
        {
             if (file == null || !file.FileName.EndsWith(".dcm", StringComparison.OrdinalIgnoreCase))
        return BadRequest("Invalid file. Only .dcm files are allowed.");

            try
            {
               
                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                var dicomFile = DicomFile.Open(tempFilePath);
                //var client = new DicomClient("127.0.0.1", 104, null,  "MY_AETITLE", "SERVER_AETITLE");
                var client = DicomClientFactory.Create("127.0.0.1", 11112, false, "MY_AETITLE", "SERVER_AETITLE");
                var request = new DicomCStoreRequest(dicomFile);
                request.OnResponseReceived += (req, response) =>
                {
                    Console.WriteLine($"C-STORE Response: {response.Status}");
                };

                await client.AddRequestAsync(request);
                await client.SendAsync();

                return Ok("DICOM file sent successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send DICOM file: {ex.Message}");
            }
        }

        [HttpPost("find")]
        public async Task<IActionResult> FindDicomStudies([FromBody] DicomQueryRequest requestModel)
        {
            if (string.IsNullOrWhiteSpace(requestModel.PatientID))
            {
                return BadRequest("PatientID is required for the query.");
            }

            try
            {
            
                var cFindRequest = new DicomCFindRequest(DicomQueryRetrieveLevel.Study)
                {
                    Dataset =
                {
                    { DicomTag.PatientID, requestModel.PatientID },
                    { DicomTag.PatientName, "" }, 
                    { DicomTag.StudyDescription, "" } 
                }
                };

                List<DicomQueryResponse> responses = new List<DicomQueryResponse>();
                cFindRequest.OnResponseReceived += (req, response) =>
                {
                    if (response.Status == DicomStatus.Success || response.Status == DicomStatus.Pending)
                    {
                        var patientID = response.Dataset?.GetString(DicomTag.PatientID);
                        var patientName = response.Dataset?.GetString(DicomTag.PatientName);
                        var studyDescription = response.Dataset?.GetString(DicomTag.StudyDescription);

                        responses.Add(new DicomQueryResponse
                        {
                            PatientID = patientID ?? "Unknown",
                            PatientName = patientName ?? "Unknown",
                            StudyDescription = studyDescription ?? "Unknown"
                        });
                    }
                };
                var client = DicomClientFactory.Create("127.0.0.1", 11112, false, "MY_AETITLE", "SERVER_AETITLE");
                await client.AddRequestAsync(cFindRequest);
                await client.SendAsync();

                if (responses.Count == 0)
                    return NotFound("No matching records found.");

                return Ok(responses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error querying DICOM server: {ex.Message}");
            }
        }
    }
    public class DicomQueryRequest
    {
        public string PatientID { get; set; }
    }
    public class DicomQueryResponse
    {
        public string PatientID { get; set; }
        public string PatientName { get; set; }
        public string StudyDescription { get; set; }
    }
}
