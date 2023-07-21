using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

using Project_CLO.Common;

using AutoMapper;

namespace Project_CLO.Services
{
    public class DiskService
    {
        private Dictionary<int, DiskInformation> _disks;
        private BlockingCollection<Func<bool>> _ioTasks = new();

        public DiskService() 
        {
            Initialize();
        }

        private void Initialize()
        {
            _disks = GetDiskListData();

            Task.Factory.StartNew(() =>
            {
                foreach (var task in _ioTasks.GetConsumingEnumerable())
                {
                    task();
                }
            });
        }

        public List<DiskInformation> GetDiskList()
        {
            return _disks.Values.ToList();
        }

        private Dictionary<int, DiskInformation> GetDiskListData()
        {
            var driveList = DriveInfo.GetDrives();
            var diskList = new Dictionary<int, DiskInformation>();

            for (int i = 0; i < driveList.Length; i++)
            {
                var driveInfo = new DiskInformation()
                {
                    Uid = i,
                    Size = driveList[i].TotalSize,
                    RootDirectoryPath = Path.Combine(driveList[i].RootDirectory.FullName, "project_CLO"),
                };

                DirectoryInfo di = new DirectoryInfo(driveInfo.RootDirectoryPath);
                if (di.Exists == false)
                    di.Create();

                diskList.Add(i, driveInfo);
            }

            return diskList;
        }

        public async Task<string> GetDirectoryStructure(int uid, int depth)
        {
            _disks.TryGetValue(uid, out var diskInformation);
            if (diskInformation == null)
                return "No disk matches uid.";

            var rootDirectoryStructure = await GetRootDirectoryStructure(diskInformation.RootDirectoryPath, depth);
            var json = string.Empty;
            JsonSerializerOptions jsonOption;

            if (depth > 30)
                jsonOption = new JsonSerializerOptions { WriteIndented = true, ReferenceHandler = ReferenceHandler.Preserve };
            else
                jsonOption = new JsonSerializerOptions { WriteIndented = true };

            json = JsonSerializer.Serialize(rootDirectoryStructure.Children, jsonOption);

            return json;
        }
        
        private async Task<ContentsStructure> GetRootDirectoryStructure(string rootDirectoryPath, int depth)
        {
            var directoriesQueue = new Queue<Tuple<DirectoryInfo, ContentsStructure, int>>();
            var rootDirectoryStructure = new ContentsStructure { Name = Path.GetFileName(rootDirectoryPath), Path = rootDirectoryPath, Children = new List<ContentsStructure>() };
            var rootDirectoryInfo = new DirectoryInfo(rootDirectoryPath);
            directoriesQueue.Enqueue(Tuple.Create(rootDirectoryInfo, rootDirectoryStructure, 0));

            while (directoriesQueue.Count > 0)
            {
                var directoryTuple = directoriesQueue.Dequeue();
                var directoryInfo = directoryTuple.Item1;
                var directoryStructure = directoryTuple.Item2;
                var currentDepth = directoryTuple.Item3;

                try
                {
                    if (currentDepth < depth)
                    {
                        foreach (var childDirectoryInfo in directoryInfo.GetDirectories())
                        {
                            var childDirectoryStructure = new ContentsStructure
                            {
                                Name = childDirectoryInfo.Name,
                                Path = childDirectoryInfo.FullName,
                                Children = new List<ContentsStructure>(),
                                Type = ContentType.Folder,
                            };
                            directoriesQueue.Enqueue(Tuple.Create(childDirectoryInfo, childDirectoryStructure, currentDepth++));
                            directoryStructure.Children.Add(childDirectoryStructure);
                        }
                    }

                    foreach (var fileInfo in directoryInfo.GetFiles())
                    {
                        var childDirectoryStructure = new ContentsStructure
                        {
                            Name = fileInfo.Name,
                            Path = fileInfo.FullName,
                            Type = ContentType.File,
                        };

                        directoryStructure.Children.Add(childDirectoryStructure);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            return rootDirectoryStructure;
        }
        
        public async Task<ContentInformation> CreateContent(int uid, RequestContentInformation request)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            _disks.TryGetValue(uid, out var diskInformation);
            if (diskInformation == null)
                return null;

            var path = Path.Combine(diskInformation.RootDirectoryPath, request.Path);

            switch (request.Type)
            {
                case ContentType.File:
                    _ioTasks.Add(() =>
                    {
                        try
                        {
                            if (request.IsOverwrite == false && File.Exists(path))
                            {
                                taskCompletionSource.SetResult(false);

                                return false;
                            }
                                

                            using (StreamWriter sw = File.CreateText(path))
                            {
                                sw.WriteLine(request.Content);
                            }

                            taskCompletionSource.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            taskCompletionSource.SetException(ex);
                        }

                        return true;
                    });
                    break;
                case ContentType.Folder:
                    _ioTasks.Add(() =>
                    {
                        if (Directory.Exists(path))
                        {
                            taskCompletionSource.SetResult(false);

                            return false;
                        }

                        Directory.CreateDirectory(path);

                        taskCompletionSource.SetResult(true);

                        return true;
                    });
                    break;
            }

            var config = new MapperConfiguration(cfg => cfg.CreateMap<RequestContentInformation, ContentInformation>());
            IMapper mapper = config.CreateMapper();

            var contentInformation = mapper.Map<ContentInformation>(request);
            contentInformation.IsCreated = await taskCompletionSource.Task;

            return contentInformation;
        }
    }
}
