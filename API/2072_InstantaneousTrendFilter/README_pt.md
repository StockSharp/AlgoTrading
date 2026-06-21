# Estratégia InstantaneousTrendFilter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa a Linha de Tendência Instantânea de John Ehlers e uma linha de gatilho para gerar sinais em qualquer período. O gatilho é calculado como `2 * ITrend - ITrend[2]`, formando uma linha rápida que cruza a linha de tendência mais lenta. Um cruzamento descendente fecha posições vendidas e abre uma comprada, enquanto um cruzamento ascendente fecha as compradas e abre uma vendida. O fator de suavização `Alpha` controla a capacidade de resposta: valores mais baixos produzem linhas mais suaves, valores mais altos reagem mais rápido.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O gatilho estava acima da linha de tendência na barra anterior e cruza abaixo na barra atual.
  - **Vendido**: O gatilho estava abaixo da linha de tendência na barra anterior e cruza acima na barra atual.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Posições compradas são fechadas quando um sinal vendido aparece.
  - Posições vendidas são fechadas quando um sinal comprado aparece.
- **Stops**: Nenhum por padrão.
- **Valores padrão**:
  - `Alpha` = 0.07.
  - `Candle Type` = Período de 4 horas.
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Simples
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
