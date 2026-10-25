# Bug 记录

| # | 日期 | 问题 | 原因 | 解决 | 状态 |
|---|---|---|---|---|---|
| 1 | 10-05 | uGUI 全部编译错误（EventSystems/UI 不存在） | `-createProject` 生成最小工程，manifest 无任何包 | 注入 com.unity.ugui 1.0.0 | ✅ |
| 2 | 10-05 | TextureImporter.Type.Sprite 编译错误 | 枚举名笔误 | TextureImporterType.Sprite | ✅ |
| 3 | 10-05 | 冒烟测试"通过"但播放模式从未运行 | 会话带 `-quit`，executeMethod 返回即退出 | 冒烟会话去掉 -quit，测试内部自行 Exit | ✅ |
| 4 | 10-05 | 冒烟测试假死（播放已启动但无人计数） | 进入播放模式触发域重载，静态事件订阅丢失 | [InitializeOnLoad]+SessionState 域重载后恢复订阅 | ✅ |
| 5 | 10-05 | 真机画面/物品模糊、太阴暗 | 无抗锯齿+低质量阴影+环境光0.10+浓雾 | 4x MSAA+阴影2048/20m+环境光0.3+线性淡雾(10~45m) | ✅ |
| 6 | 10-06 | `Inventory` 报"是命名空间不能当类型" | 命名空间与类同名，子命名空间内裸名解析优先命中命名空间 | 全限定 Door13.Inventory.Inventory | ✅ |
| 7 | 10-06 | URP 资产无 softShadowsSupported 属性 | URP14 软阴影开关在 Renderer 资产上 | SerializedObject 写 m_SupportsSoftShadows | ✅ |
| 8 | 10-06 | 冒烟日志炸弹：665秒写1.87GB日志、200万条错误 | Finish() 无幂等标记每帧重入；打印错误时收集器仍订阅（自加自打印） | 收集上限50条；Finish 幂等+先摘收集器+快照遍历 | ✅ |
| 9 | 10-06 | 冒烟正确密码未开保险箱 | 测试直接 EnterCode 未先 Open(safe)，currentSafe 为空 | 按真实流程先开密码盘再输码 | ✅ |
| 10 | 10-06 | 未激活的 LoopObject 循环规则不生效 | 未激活对象 Awake 不执行、注册不上 | LoopManager.Start 全量扫描（含未激活） | ✅ |
| 11 | 10-05 | 测试房间对象重复生成 | EnsureScene 打开既有场景后重复 Build | 改为每次 NewScene 全量重建 | ✅ |

## 遗留观察项（非Bug，待真机验证）

- 摇杆/视角灵敏度需真机手感校准
- TextMesh 中文字体依赖系统回退，低端机渲染待验证
- 门后前室为占位黑暗箱，开门2秒内淡出（设计如此，V0.5 做真实门洞）
