# Estratégia I-Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia I-Gap** replica o consultor especialista "i-GAP" do MetaTrader. Ela monitora a lacuna de preço entre o fechamento da vela anterior e a abertura da vela atual. Uma lacuna de abertura descendente que exceda um número especificado de passos de preço pode acionar uma entrada comprada e opcionalmente fechar posições vendidas existentes. Uma lacuna ascendente funciona da mesma forma para as posições vendidas.

## Detalhes
- **Critérios de entrada**: A lacuna de abertura entre velas consecutivas excede o tamanho configurado.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal de lacuna oposto.
- **Stops**: Sem stop loss ou take profit fixos.
- **Valores padrão**:
  - `CandleType` = 1 hour
  - `GapSize` = 5
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
