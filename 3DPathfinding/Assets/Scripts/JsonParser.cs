using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using SingleThread;

public class JsonParser
{
    [System.Serializable]
    public class WrapperFor3DArray
    {
        public int width, height, depth; // 3D 배열 크기 저장
        public SingleThread.Node[] data; // 1D 배열 데이터

        public WrapperFor3DArray(SingleThread.Node[,,] array)
        {
            width = array.GetLength(0);
            height = array.GetLength(1);
            depth = array.GetLength(2);

            data = new SingleThread.Node[width * height * depth];
            int index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < depth; k++)
                    {
                        data[index++] = array[i, j, k];
                    }
                }
            }
        }

        public SingleThread.Node[,,] To3DArray()
        {
            SingleThread.Node[,,] array = new SingleThread.Node[width, height, depth];

            int index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < depth; k++)
                    {
                        array[i, j, k] = data[index++];
                    }
                }
            }
            return array;
        }
    }


    //// 저장 시
    //SingleThread.Node[,,] myArray = new SingleThread.Node[3, 4, 5];
    //WrapperFor3DArray wrapper = new WrapperFor3DArray(myArray);
    //string json = JsonUtility.ToJson(wrapper);

    //// 로드 시
    //WrapperFor3DArray loadedWrapper = JsonUtility.FromJson<WrapperFor3DArray>(json);
    //int[,,] loadedArray = loadedWrapper.To3DArray();

    //public T JsonToObject<T>(string json)
    //{
    //    JsonUtility.FromJson<T>(wrapper.To3DArray()

    //    WrapperFor3DArray wrapper = new WrapperFor3DArray();
    //    return JsonUtility.FromJson<T>(wrapper.To3DArray());
    //}

    //public T JsonToObject<T>(TextAsset tmpAsset)
    //{
    //    //var setting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
    //    //return JsonConvert.DeserializeObject<T>(tmpAsset.text, setting);
    //    return JsonUtility.FromJson<T>(tmpAsset.text);
    //}

    //public string ObjectToJson(SingleThread.Node[,,] objectToParse)
    //{
    //    WrapperFor3DArray wrapper = new WrapperFor3DArray(objectToParse);
    //    return JsonUtility.ToJson(wrapper);
    //}

    //public string ObjectToJson(object objectToParse)
    //{
    //    //var setting = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
    //    //return JsonConvert.SerializeObject(objectToParse, setting);

    //    return JsonUtility.ToJson(objectToParse);
    //}

    public string ObjectToJson(SingleThread.Node[,,] nodes)
    {
        WrapperFor3DArray wrapper = new WrapperFor3DArray(nodes);
        return JsonUtility.ToJson(wrapper);
    }

    public SingleThread.Node[,,] JsonToObject(string json)
    {
        WrapperFor3DArray wrapper = JsonUtility.FromJson<WrapperFor3DArray>(json);
        SingleThread.Node[,,] nodes = wrapper.To3DArray();
        return nodes;
    }
}