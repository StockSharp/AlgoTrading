# Estratégia Clássica e-TurboFx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **e-TurboFx Classic** é uma porta C# direta do consultor especialista MetaTrader 4 encontrado em `MQL/7262/e-TurboFx.mq4`. Ele detecta a exaustão do momento após uma sequência de velas fortes com corpos progressivamente maiores e entra na direção oposta. A versão StockSharp usa a estratégia de alto nível API com assinaturas de velas, ordens de proteção automáticas e parâmetros amigáveis ​​à interface do usuário.

## Lógica de negociação
1. Assine o tipo de vela configurado e inspecione apenas as velas acabadas.
2. Meça o tamanho do corpo da vela (`|close - open|`) para detectar expansão.
3. Mantenha dois contadores:
   - **Sequência de baixa** – conta velas de baixa consecutivas com corpos maiores que a vela de baixa anterior.
   - **Sequência de alta** – conta velas de alta consecutivas com corpos maiores que a vela de alta anterior.
4. Redefina ambas as sequências quando um doji (abrir é fechar) aparecer ou sempre que uma posição já estiver aberta. Isso imita o comportamento original EA que mantém apenas uma negociação por vez.
5. **Entrada longa:** quando o comprimento da sequência de baixa atingir o `SequenceLength` configurado, envie uma ordem de compra de mercado e reinicie imediatamente os contadores.
6. **Entrada curta:** quando o comprimento da sequência de alta atingir `SequenceLength`, envie uma ordem de venda a mercado e reinicie os contadores.
7. Os níveis opcionais de stop-loss e take-profit são traduzidos das distâncias dos pontos em StockSharp etapas de preço.

O algoritmo, portanto, espera por um movimento semelhante a uma capitulação, onde cada vela acelera na mesma direção. A ordem de reversão a seguir tenta atenuar esse impulso extremo.

## Detalhes de implementação
- Usa `SubscribeCandles().Bind(ProcessCandle)` para processar velas concluídas sem gerenciamento manual de indicadores.
- Integra-se com `StartProtection` para que as distâncias de stop-loss e take-profit sejam convertidas em etapas de preço de troca (`UnitTypes.Step`).
- Os parâmetros são registrados por meio de `Param(...)` para que apareçam na IU e possam ser otimizados.
- A estratégia funciona com qualquer instrumento que exponha um `PriceStep` válido; caso contrário, as distâncias de parada/alvo devem permanecer em `0`.
- Enquanto uma posição está ativa, a detecção do sinal é pausada e os contadores internos são limpos, assim como o script MQL original que se recusou a empilhar pedidos.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `SequenceLength` | Número de velas concluídas consecutivas com corpos em expansão necessárias para acionar uma entrada. | `3` |
| `TakeProfitSteps` | Distância de take-profit medida em etapas de preço (ticks). `0` desativa o alvo. | `120` |
| `StopLossSteps` | Distância de stop-loss medida em etapas de preço (ticks). `0` desativa a parada. | `70` |
| `TradeVolume` | Volume para entradas no mercado. Alterá-lo atualiza a propriedade `Volume` instantaneamente. | `0.1` |
| `CandleType` | Período de vela usado para análise. O padrão é velas de 1 hora. | `1 hour` |

## Notas de uso
- A estratégia espera dados limpos de velas. Ao trocar de instrumentos ou prazos, permita que os caches sejam reconstruídos para que os contadores reflitam apenas velas novas.
- Como o sistema depende da expansão estrita do corpo, corpos de velas minúsculos ou iguais redefinem a sequência. Ajuste `SequenceLength` ao negociar em períodos de tempo mais ruidosos.
- Faça backtest de múltiplas combinações de período/volume para encontrar instrumentos onde os movimentos de exaustão são frequentes o suficiente para compensar spreads e derrapagens.
- Sempre valide o comportamento em um ambiente sandbox antes de ativar a negociação ao vivo.
