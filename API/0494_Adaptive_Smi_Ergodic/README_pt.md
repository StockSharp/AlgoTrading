# Estratégia SMI Ergódica Adaptativa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia SMI Ergódica Adaptativa usa o oscilador True Strength Index (TSI) com uma linha de sinal EMA para detectar reversões de extremos de sobrecompra ou sobrevenda. Uma posição comprada é aberta quando o TSI cruza acima do limiar de sobrevenda enquanto permanece acima de sua linha de sinal. Uma posição vendida é aberta quando o TSI cruza abaixo do limiar de sobrecompra e está abaixo da linha de sinal.

## Detalhes

- **Critérios de entrada**:
  - TSI cruza acima da sobrevenda e TSI > sinal (comprado).
  - TSI cruza abaixo da sobrecompra e TSI < sinal (vendido).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal reverso aciona a operação oposta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `LongLength` = 12
  - `ShortLength` = 5
  - `SignalLength` = 5
  - `OversoldThreshold` = -0.4
  - `OverboughtThreshold` = 0.4
- **Filtros**:
  - Categoria: Oscilador de momentum
  - Direção: Comprado/Vendido
  - Indicadores: True Strength Index, EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
