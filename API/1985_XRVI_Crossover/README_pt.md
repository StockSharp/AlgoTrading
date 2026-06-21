# Estratégia de Cruzamento XRVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Cruzamento XRVI é baseada no Extended Relative Vigor Index (XRVI).
O XRVI é calculado suavizando o Relative Vigor Index e depois aplicando uma segunda média móvel para produzir uma linha de sinal.
A estratégia entra comprada quando o XRVI cruza acima da linha de sinal e entra vendida quando cruza abaixo.
As posições existentes são revertidas em sinais opostos.

## Detalhes

- **Critérios de entrada**: Cruzamento do XRVI com sua linha de sinal
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = Período H4
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Relative Vigor Index, Simple Moving Average
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
