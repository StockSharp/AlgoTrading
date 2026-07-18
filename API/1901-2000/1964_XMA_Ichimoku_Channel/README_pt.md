# Estratégia de Canal XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia implementa um sistema de rompimento de canal baseado no conceito XMA Ichimoku. Ela constrói um canal dinâmico em torno de uma média suavizada das máximas e mínimas recentes e gera operações quando a ação do preço confirma um rompimento com um recuo.

## Como funciona

1. **Valores máximos e mínimos**: Para cada vela finalizada, a estratégia calcula a máxima mais alta e a mínima mais baixa ao longo de períodos de lookback configuráveis.
2. **Linha central suavizada**: O ponto médio entre os valores máximo e mínimo é suavizado usando uma média móvel simples.
3. **Construção do canal**: As bandas superior e inferior são derivadas da linha central suavizada pela aplicação de deslocamentos percentuais.
4. **Lógica de trading**:
   - Se o fechamento anterior estava acima da banda superior anterior e o fechamento atual retorna abaixo da banda superior atual, a estratégia abre uma posição comprada e fecha qualquer posição vendida existente.
   - Se o fechamento anterior estava abaixo da banda inferior anterior e o fechamento atual retorna acima da banda inferior atual, a estratégia abre uma posição vendida e fecha qualquer posição comprada existente.

## Parâmetros

- **Up Period** – período de lookback para o preço mais alto.
- **Down Period** – período de lookback para o preço mais baixo.
- **MA Length** – comprimento da média móvel de suavização.
- **Up Percent** – percentual adicionado à linha central para formar a banda superior.
- **Down Percent** – percentual subtraído da linha central para formar a banda inferior.
- **Candle Type** – período dos candles usados nos cálculos.

## Notas de uso

- As operações são executadas com ordens a mercado.
- Apenas velas finalizadas são processadas para evitar sinais falsos.
- A estratégia fecha posições opostas antes de abrir uma nova.

## Aviso

Este exemplo é fornecido apenas para fins educacionais. Teste exaustivamente antes de usar em trading ao vivo.
