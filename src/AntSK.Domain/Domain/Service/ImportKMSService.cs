﻿using AntSK.Domain.Common.DependencyInjection;
using AntSK.Domain.Domain.Interface;
using AntSK.Domain.Domain.Model;
using AntSK.Domain.Domain.Model.Constant;
using AntSK.Domain.Domain.Model.Excel;
using AntSK.Domain.Domain.Other;
using AntSK.Domain.Repositories;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;
using System.Text;

namespace AntSK.Domain.Domain.Service
{
    [ServiceDescription(typeof(IImportKMSService), ServiceLifetime.Scoped)]
    public class ImportKMSService(
        IKMService _kMService,
        IKmsDetails_Repositories _kmsDetails_Repositories,
        IKmss_Repositories _kmss_Repositories
        ) : IImportKMSService
    {

        public void ImportKMSTask(ImportKMSTaskReq req)
        {
            try
            {
                var km = _kmss_Repositories.GetFirst(p => p.Id == req.KmsId);

                var _memory = _kMService.GetMemoryByKMS(km.Id);
                string fileid = req.KmsDetail.Id;
                switch (req.ImportType)
                {
                    case ImportType.File:
                        //导入文件
                        {
                            var importResult = _memory.ImportDocumentAsync(new Document(fileid)
                          .AddFile(req.FilePath)
                          .AddTag(KmsConstantcs.KmsIdTag, req.KmsId)
                          , index: KmsConstantcs.KmsIndex).Result;
                            //查询文档数量
                            var docTextList = _kMService.GetDocumentByFileID(km.Id, fileid).Result;
                            string fileGuidName = Path.GetFileName(req.FilePath);
                            req.KmsDetail.FileName = req.FileName;
                            req.KmsDetail.FileGuidName = fileGuidName;
                            req.KmsDetail.DataCount = docTextList.Count;

                        }
                        break;
                    case ImportType.Url:
                        {
                            //导入url                  
                            var importResult = _memory.ImportWebPageAsync(req.Url, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                 , index: KmsConstantcs.KmsIndex).Result;
                            //查询文档数量
                            var docTextList = _kMService.GetDocumentByFileID(km.Id, fileid).Result;
                            req.KmsDetail.Url = req.Url;
                            req.KmsDetail.DataCount = docTextList.Count;
                        }
                        break;
                    case ImportType.Text:
                        //导入文本
                        {
                            var importResult = _memory.ImportTextAsync(req.Text, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                , index: KmsConstantcs.KmsIndex).Result;
                            //查询文档数量
                            var docTextList = _kMService.GetDocumentByFileID(km.Id, fileid).Result;
                            req.KmsDetail.Url = req.Url;
                            req.KmsDetail.DataCount = docTextList.Count;

                        }
                        break;
                    case ImportType.Excel:
                        using (var fs = File.OpenRead(req.FilePath))
                        {
                            var excelList= ExeclHelper.ExcelToList<KMSExcelModel>(fs);
                            
                            _memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
                            _memory.Orchestrator.AddHandler<KMExcelHandler>("antsk_excel_split");
                            _memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
                            _memory.Orchestrator.AddHandler<SaveRecordsHandler>("save_memory_records");

                            StringBuilder text = new StringBuilder();
                            foreach (var item in excelList)
                            {
                                text.AppendLine(@$"Question:{item.Question}{Environment.NewLine}Answer:{item.Answer}{KmsConstantcs.KMExcelSplit}");                            
                            }
                            var importResult = _memory.ImportTextAsync(text.ToString(), fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                  , index: KmsConstantcs.KmsIndex,
                                  steps: new[]
                                  {
                                        "extract_text",
                                        "antsk_excel_split",
                                        "generate_embeddings",
                                        "save_memory_records"
                                  }
                                  ).Result;
                            req.KmsDetail.FileName = req.FileName;
                            string fileGuidName = Path.GetFileName(req.FilePath);
                            req.KmsDetail.FileGuidName = fileGuidName;
                            req.KmsDetail.DataCount = excelList.Count();
                        }                        
                        break;
                }
                req.KmsDetail.Status = Model.Enum.ImportKmsStatus.Success;
                _kmsDetails_Repositories.Update(req.KmsDetail);
                //_kmsDetails_Repositories.GetList(p => p.KmsId == req.KmsId);
                Console.WriteLine("后台导入任务成功:" + req.KmsDetail.DataCount);
            }
            catch (Exception ex)
            {
                req.KmsDetail.Status = Model.Enum.ImportKmsStatus.Fail;
                _kmsDetails_Repositories.Update(req.KmsDetail);
                Console.WriteLine("后台导入任务异常:" + ex.Message);
            }
        }
    }
}
