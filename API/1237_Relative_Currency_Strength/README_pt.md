# Força Relativa de Moedas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Força Relativa de Moedas compara um par de moedas com uma cesta das principais divisas.
Compra quando o par negociado supera a média das outras principais e vende quando fica abaixo.
A comparação é baseada na variação percentual desde o início da sessão.

## Detalhes

- **Critérios de entrada**: a força do par principal supera a média pelo limiar.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: a força cai abaixo da média pelo limiar.
- **Stops**: Não.
- **Valores padrão**:
  - `Threshold` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Variação de preço
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
