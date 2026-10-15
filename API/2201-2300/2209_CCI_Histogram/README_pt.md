# Estratégia de Histograma CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Commodity Channel Index (CCI) para detectar reversões quando o indicador sai de zonas extremas. Uma posição comprada é aberta quando o CCI cai abaixo do nível superior após ter estado acima dele. Uma posição vendida é aberta quando o CCI sobe acima do nível inferior após ter estado abaixo dele. Níveis opcionais de stop loss e take profit em pontos podem proteger posições abertas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: CCI anterior > `UpperLevel` e CCI atual ≤ `UpperLevel`.
  - **Vendido**: CCI anterior < `LowerLevel` e CCI atual ≥ `LowerLevel`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O sinal oposto fecha a posição existente e abre uma nova.
- **Stops**: Stop loss e take profit fixos opcionais em pontos.
- **Valores padrão**:
  - `CCI Period` = 14
  - `Upper Level` = 100
  - `Lower Level` = -100
  - `Stop Loss` = 100 pontos
  - `Take Profit` = 200 pontos
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: CCI
  - Stops: Opcional
  - Complexidade: Simples
  - Período: Qualquer (padrão 4H)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

