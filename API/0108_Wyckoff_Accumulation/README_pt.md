# Estratégia de Acumulação Wyckoff
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A Acumulação Wyckoff descreve uma fase de base onde grandes interesses constroem posições silenciosamente após uma queda.
O volume e a ação do preço formam uma série de testes do suporte seguidos de mínimas mais altas, sugerindo demanda crescente.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

Esta estratégia entra comprado quando o preço rompe o range de acumulação, esperando uma nova tendência de alta alimentada por essas compras anteriores.

Um stop protetor fica logo abaixo da base para limitar perdas caso o rompimento falhe.

## Detalhes

- **Critérios de entrada**: sinal de indicador
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Volume, Price
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

