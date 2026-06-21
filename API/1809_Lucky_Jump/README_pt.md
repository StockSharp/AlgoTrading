# Estratégia Lucky Jump
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Lucky Jump é um sistema de reversão à média de curto prazo que reage a saltos repentinos de preço no melhor bid e ask. Quando o preço ask salta para cima por um número específico de pontos em comparação com a cotação anterior, a estratégia abre uma posição vendida esperando um recuo. Por outro lado, quando o preço bid cai pelo mesmo valor, ela entra comprada. As posições são fechadas no primeiro tick favorável ou quando a perda excede um limite predefinido.

Esta abordagem tenta capturar correções rápidas após movimentos agressivos do mercado. Opera puramente em dados de cotação Level1 e não depende de velas ou indicadores.

## Detalhes

- **Critérios de entrada**:
  - **Vendido**: `Ask(t) - Ask(t-1) >= Shift * PriceStep`.
  - **Comprado**: `Bid(t-1) - Bid(t) >= Shift * PriceStep`.
- **Critérios de saída**:
  - Fechar a posição assim que se torne lucrativa.
  - Fechar se a perda exceder `Limit * PriceStep`.
- **Stops**: stop implícito baseado no parâmetro `Limit`.
- **Valores padrão**:
  - `Shift` = 30 pontos.
  - `Limit` = 180 pontos.
  - `Volume` = 1.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Ultra curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

