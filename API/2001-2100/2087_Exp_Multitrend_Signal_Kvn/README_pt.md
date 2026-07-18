# Estratégia Exp Multitrend Signal KVN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o conceito MultiTrend Signal KVN. Ela constrói um canal de preços adaptativo usando o Average Directional Index (ADX) para determinar a janela de retrospectiva. Quando o preço fecha acima do canal, a estratégia abre uma posição comprada. Quando o preço fecha abaixo do canal, abre uma posição vendida.

A largura do canal é definida pelo parâmetro **K** como percentual da oscilação entre máximas e mínimas recentes. **KPeriod** define o número base de barras usadas para os cálculos, enquanto o valor do ADX dimensiona a janela real. **KStop** multiplica o intervalo médio e é adicionado às operações de rompimento para determinar a distância do stop.

A estratégia é projetada para negociação tanto comprada quanto vendida e usa o período de 4 horas por padrão. Não são fornecidos stop loss ou take profit explícitos; a proteção pode ser habilitada pela plataforma.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço de fechamento rompe acima da banda adaptativa superior.
  - **Vendido**: O preço de fechamento rompe abaixo da banda adaptativa inferior.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal reverso na direção oposta.
- **Stops**: Opcional via proteção da estratégia.
- **Valores padrão**:
  - `K` = 48
  - `KStop` = 0.5
  - `KPeriod` = 150
  - `AdxPeriod` = 14
  - `Tipo de vela` = velas de 4 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ADX, SMA, Max/Min
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
