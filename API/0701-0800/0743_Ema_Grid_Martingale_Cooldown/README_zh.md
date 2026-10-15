# EMA网格马丁策略（带冷却）
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

基于EMA的仅做多网格策略，可选马丁加仓，并在每轮网格之间设置冷却期。当两组快速EMA同时上穿慢速EMA时开启新网格，价格按固定点差下跌时继续买入，价格达到加权平均价加缓冲后平仓。
