﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathNode
{
    public int parent;//上一层节点.
    public int wayPointIdx;
}

public enum WalkType
{
    Normal = 0, 
    Jump = 1,
}

public class PathMng:Singleton<PathMng>
{
    #region 寻路缓存
    List<int> looked = new List<int>();

    public void FindPath(MeteorUnit user, Vector3 source, Vector3 target, ref List<WayPoint> wp)
    {
        int startPathIndex = GetWayIndex(source);
        int endPathIndex = GetWayIndex(target);
        looked.Clear();
        wp.Clear();
        FindPathCore(user, startPathIndex, endPathIndex, ref wp);
    }

    //public void FindPath(Vector3 now, MeteorUnit user, MeteorUnit target, ref List<WayPoint> wp)
    //{
    //    int startPathIndex = GetWayIndex(now);
    //    Vector3 vec = target.mPos;
    //    int endPathIndex = GetWayIndex(vec);
    //    looked.Clear();
    //    FindPathCore(startPathIndex, endPathIndex);
    //}

    public WalkType GetWalkMethod(int start, int end)
    {
        if (Global.GLevelItem.wayPoint.Count > start && Global.GLevelItem.wayPoint.Count > end)
        {
            if (Global.GLevelItem.wayPoint[start].link.ContainsKey(end))
            {
                return (WalkType)Global.GLevelItem.wayPoint[start].link[end].mode;
            }
            //else
            //    Debug.LogError(string.Format("{0}-{1} can not link", start, end));
        }
        return WalkType.Normal;
    }

    public void FindPath(MeteorUnit user, int start, int end, ref List<WayPoint> wp)
    {
        looked.Clear();
        wp.Clear();
        FindPathCore(user, start, end, ref wp);
    }

    

    
    void FindPathCore(MeteorUnit user, int start, int end, ref List<WayPoint> wp)
    {
        if (looked.Contains(start))
            return;
        //Debug.Log(string.Format("start:{0}:end:{1}", start, end));
        looked.Add(start);
        if (start == -1 || end == -1)
            return;
        if (start == end)
        {
            wp.Add(Global.GLevelItem.wayPoint[start]);
            return;
        }

        if (Global.GLevelItem.DisableFindWay == 1)
        {
            List<WayPoint> direct = new List<WayPoint>();
            wp.Add(Global.GLevelItem.wayPoint[start]);
            wp.Add(Global.GLevelItem.wayPoint[end]);
            return;
        }

        //从开始点，跑到最终点，最短线路？
        if (Global.GLevelItem.wayPoint[start].link.ContainsKey(end))
        {
            wp.Add(Global.GLevelItem.wayPoint[start]);
            wp.Add(Global.GLevelItem.wayPoint[end]);
            return;
        }
        else
        {
            //深度优先递归.并非最短
            //Dictionary<int, WayLength> ways = Global.GLevelItem.wayPoint[start].link;
            //foreach (var each in ways)
            //{
            //    List<WayPoint> p = FindPathCore(each.Key, end);
            //    if (p != null && p.Count != 0)
            //    {
            //        path.Add(Global.GLevelItem.wayPoint[start]);
            //        for (int i = 0; i < p.Count; i++)
            //            path.Add(p[i]);
            //        return path;
            //    }
            //}

            if (true)
            {
                //收集路径信息 层次
                user.robot.PathInfo.Clear(); 
                PathNode no = user.robot.nodeContainer[start];
                no.wayPointIdx = start;
                user.robot.PathInfo.Add(0, new List<PathNode>() { no });
                CollectPathInfo(user, start, end);

                //Debug.LogError(string.Format("寻路起始点:{0}-寻路终止点:{1}", start, end));
                //foreach (var each in user.robot.PathInfo)
                //{
                //    Debug.Log(string.Format("layer:{0}", each.Key));
                //    for (int i = 0; i < each.Value.Count; i++)
                //        Debug.Log(string.Format("{0}", each.Value[i].wayPointIdx));
                //}

                //计算最短路径.从A-B，路径越少，越短，2边之和大于第3边
                int target = end;
                float tick = Time.timeSinceLevelLoad;
                bool goOut = false;
                while (true)
                {
                    if (Time.timeSinceLevelLoad - tick > 10.0f)
                    {
                        Debug.LogError("find path time up");
                        Debug.DebugBreak();
                        break;
                    }

                    bool find = false;
                    foreach (var each in user.robot.PathInfo)
                    {
                        for (int i = 0; i < each.Value.Count; i++)
                        {
                            if (each.Value[i].wayPointIdx == target)
                            {
                                find = true;
                                if (wp.Count == 0)
                                    wp.Add(Global.GLevelItem.wayPoint[target]);
                                else
                                    wp.Insert(0, Global.GLevelItem.wayPoint[target]);
                                while (each.Value[i].parent != start)
                                {
                                    target = each.Value[i].parent;
                                    break;
                                }
                                if (each.Value[i].parent == start)
                                    goOut = true;
                                //goOut = true;
                                break;
                            }
                        }
                        if (find)
                            break;
                    }

                    if (!find)
                    {
                        Debug.LogError(string.Format("孤立的寻路点:{0},没有点可以走向他", target));
                        if (wp.Count == 0)
                            wp.Add(Global.GLevelItem.wayPoint[target]);
                        else
                            wp.Insert(0, Global.GLevelItem.wayPoint[target]);
                        break;
                    }

                    if (goOut)
                        break;
                }
                wp.Insert(0, Global.GLevelItem.wayPoint[start]);
            }
        }
    }

    //查看之前层级是否已统计过该节点信息
    bool PathLayerExist(MeteorUnit user, int wayPoint)
    {
        foreach (var each in user.robot.PathInfo)
        {
            for (int i = 0; i < each.Value.Count; i++)
            {
                if (each.Value[i].wayPointIdx == wayPoint)
                    return true;
            }
        }
        return false;
    }

    //从起点开始 构造寻路树.
    void CollectPathInfo(MeteorUnit user, int start, int end, int layer = 1)
    {
        CollectPathLayer(user, start, end, layer);
        while (user.robot.PathInfo.ContainsKey(layer))
        {
            int nextLayer = layer + 1;
            for (int i = 0; i < user.robot.PathInfo[layer].Count; i++)
            {
                CollectPathLayer(user, user.robot.PathInfo[layer][i].wayPointIdx, end, nextLayer);
            }
            layer = nextLayer;
        }
    }

    //收集从起点到终点经过的所有层级路点,一旦遇见最近层级的终点就结束，用于计算最短路径.
    void CollectPathLayer(MeteorUnit user, int start, int end, int layer = 1)
    {
        Dictionary<int, WayLength> ways = Global.GLevelItem.wayPoint[start].link;
        foreach (var each in ways)
        {
            if (!PathLayerExist(user, each.Key))
            {
                //之前的所有层次中并不包含此节点.
                PathNode no = user.robot.nodeContainer[each.Key];
                no.wayPointIdx = each.Key;
                no.parent = start;
                if (user.robot.PathInfo.ContainsKey(layer))
                    user.robot.PathInfo[layer].Add(no);
                else
                    user.robot.PathInfo.Add(layer, new List<PathNode> { no });
            }
        }
    }

    //得到当前位置所处路点临近的路点其中之一
    public Vector3 GetNearestWayPoint(Vector3 vec)
    {
        int start = GetWayIndex(vec);
        if (Global.GLevelItem.wayPoint.Count > start && start >= 0)
        {
            if (Global.GLevelItem.wayPoint[start].link != null)
            {
                List<int> ret = Global.GLevelItem.wayPoint[start].link.Keys.ToList();
                int k = Random.Range(0, ret.Count);
                if (ret.Count != 0)
                    return Global.GLevelItem.wayPoint[ret[k]].pos;
            }
        }
        return Vector3.zero;
    }

    public int GetWayIndex(Vector3 now)
    {
        int ret = -1;
        float min = 10000.0f;
        for (int i = 0; i < Global.GLevelItem.wayPoint.Count; i++)
        {
            WayPoint way = Global.GLevelItem.wayPoint[i];
            float dis = Vector3.Distance(way.pos, now);
            //if (dis <= way.size)
            //    return i;
            if (dis < min)
            {
                min = dis;
                ret = i;
            }
        }
        return ret;
    }
    #endregion
}
