# Estratégia Boilerplate Configurável
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Boilerplate Configurável pode alternar entre dois modos: um cruzamento de médias móveis simples ou um rompimento por compressão de Bandas de Bollinger. Inclui filtros de dia de operação e sessão, um intervalo de datas, uma janela de notícias e gerenciamento de risco usando ATR ou risco/retorno estático.

## Detalhes

- **Critérios de entrada**:
  - No modo `SmaCross`, vai comprado quando a SMA rápida cruza acima da SMA lenta e vendido no cruzamento oposto.
  - No modo `Squeeze`, entra quando o preço rompe a banda de Bollinger externa enquanto permanece dentro da banda mais estreita.
- **Comprado/Vendido**: Configurável para comprado, vendido ou ambos com inversão opcional.
- **Critérios de saída**:
  - Stop loss e take profit baseados em ATR ou percentuais estáticos.
  - O período de saída diário e a janela de notícias fecham todas as posições.
- **Stops**: Stop loss e take profit por operação com proteção de drawdown.
- **Valores padrão**:
  - `Length` = 20
  - `WideMultiplier` = 1.5
  - `NarrowMultiplier` = 2
  - `MaxLossPerc` = 0.02
  - `AtrMultiplier` = 1.5
  - `StaticRr` = 2
  - `NewsWindow` = 5
  - `MaxDrawdown` = 0.1
- **Filtros**:
  - Categoria: Modular
  - Direção: Comprado e Vendido
  - Indicadores: SMA, Bollinger Bands, ATR
  - Stops: Sim
  - Complexidade: Avançado
  - Período: Qualquer
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
