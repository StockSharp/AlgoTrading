# Estratégia de Cruzamento Bulls & Bears Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento dos indicadores Bulls Power e Bears Power em um período de quatro horas. Bulls Power mede a pressão compradora acima de um preço médio, enquanto Bears Power mostra a pressão vendedora abaixo dele. Quando a força compradora supera a força vendedora, o sistema abre uma posição comprada. Quando a força vendedora se torna dominante, abre uma posição vendida.

Testes em dados históricos de criptomoedas mostram que cruzamentos claros frequentemente precedem reversões de curto prazo. A estratégia é projetada para estar sempre comprada ou vendida, revertendo a posição sempre que os indicadores se cruzam na direção oposta.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O valor de Bulls Power cruza acima de Bears Power.
  - **Vendido**: O valor de Bears Power cruza acima de Bulls Power.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Cruzamento oposto que reverte a posição.
- **Stops**: Nenhum. As posições são revertidas em vez de encerradas por stop.
- **Filtros**:
  - Período: velas de 4 horas por padrão.
  - Indicadores: Bulls Power, Bears Power.
  - Direção: Reversão baseada em mudança de momentum.
