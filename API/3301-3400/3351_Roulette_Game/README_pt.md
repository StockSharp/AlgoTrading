# Jogo de Roleta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia do Jogo da Roleta recria o consultor especialista semelhante ao cassino de MetaTrader dentro de StockSharp. Ele trata cada vela finalizada como um novo giro da roda, escolhe uma direção aleatória e dimensiona o tamanho do pedido após perdas usando uma progressão no estilo Martingale. A implementação monitora um saldo virtual e limita a exposição por meio de limites configuráveis.

Cada rodada começa achatando qualquer posição existente, lançando uma moeda virtual para representar vermelho ou preto e enviando uma ordem de mercado na direção selecionada. Quando a próxima vela fecha, a estratégia verifica se o fechamento foi favorável à aposta. As vitórias repõem a aposta no volume base, enquanto as perdas multiplicam a aposta até um limite máximo definido. Uma proteção máxima de sequência de derrotas força uma reinicialização antes que a exposição se torne extrema. Velas de resfriamento opcionais podem ser inseridas entre as rodadas para diminuir o ritmo das apostas.

Esta conversão concentra-se na gestão de dinheiro inspirada no jogo, apresentada pelo especialista original, em vez de sinais indicadores. Ele demonstra como orquestrar rodadas baseadas em tempo, manter o estado interno e interagir com o API de alto nível de API por meio de assinaturas de velas.

## Detalhes

- **Critérios de entrada**: Sem filtro técnico. A direção é selecionada aleatoriamente no final de uma vela acabada.
- **Longo/Curto**: Ambas as direções, escolhidas aleatoriamente a cada rodada.
- **Critérios de Saída**: A posição fecha na próxima vela finalizada, avaliando se o preço fechou acima ou abaixo da entrada.
- **Paradas**: Sem paradas tradicionais. O risco é gerenciado com limites de aposta e limites de sequência.
- **Valores padrão**:
  - `BaseVolume` = 1m
  - `LossMultiplier` = 2m
  - `MaxMultiplier` = 16m
  - `RoundCooldown` = 1
  - `MaxLosingStreak` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Gestão de dinheiro
  - Direção: Ambos
  - Indicadores: Nenhum
  - Paradas: Não
  - Complexidade: Iniciante
  - Prazo: Curto prazo
  - Sazonalidade: Não
  - Redes Neurais: Não
  - Divergência: Não
  - Nível de risco: Alto

## Notas

- As ordens de mercado são dimensionadas de acordo com a aposta ajustada pelo multiplicador e arredondadas para o nível de volume do instrumento.
- As vitórias redefinem a aposta para o volume base; as perdas aumentam pelo multiplicador até que o multiplicador máximo ou o limite de sequência de derrotas seja atingido.
- As barras de resfriamento evitam a reentrada imediata e possibilitam a sincronização com feeds de dados mais lentos.
