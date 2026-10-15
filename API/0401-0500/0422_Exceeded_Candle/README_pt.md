# Estratégia de Exceeded Candle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem baseada em padrões procura velas de engolfo altista que excedam a barra anterior enquanto o preço ainda está abaixo da banda média de Bollinger. A ideia é que uma forte reversão dentro de um recuo pode impulsionar o preço de volta à banda superior. A estratégia opera apenas comprado e cancela entradas quando três velas baixistas consecutivas aparecem.

Sempre que o preço atinge a banda superior de Bollinger, a posição é fechada, capturando o rápido recuo. O método é adequado para períodos curtos onde as bandas de volatilidade capturam oscilações de reversão à média.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: vela anterior vermelha, vela atual verde e fecha acima da abertura anterior, `Close < MiddleBand`, sem três velas vermelhas consecutivas
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - **Comprado**: `Close > UpperBand`
- **Stops**: Nenhum
- **Valores padrão**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Somente comprado
  - Indicadores: Bollinger Bands, price action
  - Stops: Não
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
