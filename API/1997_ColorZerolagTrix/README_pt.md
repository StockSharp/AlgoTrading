# Estratégia Color Zerolag TRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia agrega cinco indicadores TRIX com diferentes períodos e pesos para produzir uma linha rápida e uma linha lenta suavizada. As operações são acionadas quando a linha rápida cruza a linha lenta.

- **Entrada comprada:** linha rápida anterior > linha lenta anterior e rápida atual < lenta atual.
- **Entrada vendida:** linha rápida anterior < linha lenta anterior e rápida atual > lenta atual.
- **Gerenciamento de posição:** flags opcionais permitem ativar ou desativar entradas e saídas compradas/vendidas separadamente.
- **Parâmetros:** fator de suavização e cinco pares de períodos TRIX com pesos correspondentes.
- **Indicadores:** TRIX (cinco instâncias) com soma ponderada e suavização.
- **Período padrão:** candles de 4 horas.

## Filtros
- Categoria: Seguidor de tendência
- Direção: Ambos
- Indicadores: Múltiplos
- Stops: Não
- Complexidade: Moderado
- Período: Longo prazo
- Sazonalidade: Não
- Redes neurais: Não
- Divergência: Não
- Nível de risco: Médio
