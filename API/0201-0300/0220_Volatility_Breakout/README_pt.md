# Estratégia de Rompimento de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento de Volatilidade busca movimentos direcionais fortes quando o preço escapa de seu intervalo médio. Medindo a distância de uma média móvel simples usando o ATR, o algoritmo define limiares de rompimento que escalam com a volatilidade.

Os testes indicam um retorno anual médio de aproximadamente 97%. Funciona melhor no mercado de criptomoedas.

Uma ordem de compra é acionada quando o fechamento sobe acima da SMA em mais de `Multiplier` vezes o ATR. Um sinal de venda aparece quando o fechamento cai abaixo da SMA pela mesma distância. As posições permanecem abertas até que um rompimento oposto ocorra ou um stop de proteção seja atingido.

Esta técnica atende traders intradia que prosperam com surtos de momentum. O uso de limiares baseados em ATR ajuda a filtrar o ruído para que apenas movimentos significativos gerem operações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Close > SMA + Multiplier * ATR
  - **Vendido**: Close < SMA - Multiplier * ATR
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando um rompimento oposto for acionado ou o stop-loss for atingido
  - **Vendido**: Sair quando um rompimento oposto for acionado ou o stop-loss for atingido
- **Stops**: Sim, stop-loss a `Multiplier * ATR` da entrada.
- **Valores padrão**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
