using FellowOakDicom;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

GenerateDicomFile();

app.Run();

void GenerateDicomFile()
{
    var dicomDataset = new DicomDataset
    {
        { DicomTag.PatientID, "123456" },
        { DicomTag.PatientName, "John Doe" },
        { DicomTag.StudyDescription, "Test Study" },
        { DicomTag.Modality, "CT" },
        { DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage },
          { DicomTag.SOPInstanceUID, DicomUID.Generate() }
    };

    string filePath = @"D:\test.dcm";
    var dicomFile = new DicomFile(dicomDataset);
    dicomFile.Save(filePath);
    Console.WriteLine($"DICOM file created at: {filePath}");
}
