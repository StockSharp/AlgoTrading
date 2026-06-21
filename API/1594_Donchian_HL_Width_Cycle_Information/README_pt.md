# Informação de Ciclo de Largura HL de Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera com base na largura do canal Donchian e nas mudanças de ciclo.

A estratégia monitora a relação dos extremos das velas com o canal Donchian. Após um ciclo de baixa, atingir a banda superior abre uma posição comprada. Após um ciclo de alta, tocar a banda inferior abre uma posição vendida.

## Detalhes

- **Critérios de entrada**: Mudança de tendência de ciclo no canal Donchian.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal de ciclo oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 28
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Donchian
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
