# Estratégia de Vela de Rejeição
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Uma Vela de Rejeição se forma quando o preço sonda um nível mas não consegue se manter além dele, deixando uma longa mecha e um corpo pequeno.
Essas velas indicam que uma tentativa de se mover em uma direção foi firmemente rejeitada pelo mercado.

Os testes indicam um retorno anual médio de aproximadamente 49%. Funciona melhor no mercado de criptomoedas.

A estratégia entra na direção oposta à mecha assim que a vela fecha, esperando que o preço reverta de volta pelo range.

Os stops são colocados fora da máxima ou mínima rejeitada para limitar o risco, e as operações são encerradas se o impulso não se materializar.

## Detalhes

- **Critérios de entrada**: correspondência de padrão
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Candlestick
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

