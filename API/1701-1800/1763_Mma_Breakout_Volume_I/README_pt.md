# Estratégia MMA de Rompimento por Volume I
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos quando o preço de fechamento cruza uma Média Móvel Suavizada (SMMA) de longo prazo.
Uma posição comprada é aberta quando o preço sobe acima da SMMA e uma posição vendida é aberta quando cai abaixo.
As posições são encerradas quando o preço se move contra a operação e cruza uma Média Móvel Exponencial (EMA).

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento cruza acima de SMMA(200).
  - **Vendido**: O preço de fechamento cruza abaixo de SMMA(200).
- **Critérios de saída**:
  - **Comprado**: O preço de fechamento cai abaixo de EMA(5).
  - **Vendido**: O preço de fechamento sobe acima de EMA(5).
- **Comprado/Vendido**: Ambos.
- **Stops**: Sem stop-loss fixo, a saída é determinada pelo sinal EMA.
- **Valores padrão**:
  - `SMMA period` = 200
  - `EMA period` = 5
  - `Candle type` = velas de 5 minutos
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias Móveis
  - Stops: Não
  - Complexidade: Simples
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
